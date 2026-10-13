using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MitsubishiApi.Data;
using MitsubishiApi.Dtos;
using MitsubishiApi.Services;

namespace MitsubishiApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class SearchController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ElasticSearchService _elastic;

    public SearchController(AppDbContext db, ElasticSearchService elastic)
    {
        _db = db;
        _elastic = elastic;
    }

    // POST: api/search/reindex = sync data dari DB ke Elasticsearch
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        var vehicles = await _db.Vehicles
            .Include(v => v.VehicleModel)
            .Include(v => v.Customer )
            .ToListAsync();
        
        var documents = vehicles.Select(v => new VehicleSearchDocument
        {
            Id = v.Id,
            LicensePlate = v.LicensePlate,
            Variant = v.Variant,
            Color = v.Color,
            VehicleModelName = v.VehicleModel?.Name ?? "",
            CustomerName = v.Customer?.FullName ?? "",
            CustomerPhone = v.Customer?.Phone ?? ""

        }).ToList();

        //hapus index lama (kalau ada), lalu buat baru
        await _elastic.Client.Indices.DeleteAsync(_elastic.DefaultIndex);
        await _elastic.Client.Indices.CreateAsync(_elastic.DefaultIndex);

        //bulk index semua dokumen
        var bulkResponse = await _elastic.Client.BulkAsync(b => b
        .Index(_elastic.DefaultIndex)
        .IndexMany(documents, (descriptor, doc) => descriptor.Id(doc.Id)));

        if (bulkResponse.Errors)
            return BadRequest(new {message = "Gagak indexing beberapa dokumen"});
        
        return Ok(new {message = $"Berhasil index {documents.Count} kendaraan"});
    }

    //GET
    [HttpGet]
    public async Task<ActionResult<List<VehicleSearchDocument>>> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Query tidak boleh kosong" });

        var response = await _elastic.Client.SearchAsync<VehicleSearchDocument>(s => s
            .Indices(_elastic.DefaultIndex)
            .From(0)
            .Size(20)
            .Query(query => query
                .MultiMatch(mm => mm
                    .Query(q)
                    .Fields(new[]
                    {
                        "licensePlate",
                        "variant",
                        "color",
                        "vehicleModelName",
                        "customerName",
                        "customerPhone"
                    })
                    // .Fuzziness DIHAPUS dulu
                )
            )
        );

        // Log untuk debug
        Console.WriteLine($"DEBUG: Search '{q}' → {response.Documents.Count} hasil");
        if (response.IsValidResponse == false)
        {
            Console.WriteLine($"DEBUG: ES error: {response.DebugInformation}");
        }

        var results = response.Documents.ToList();
        return Ok(results);
    }
}