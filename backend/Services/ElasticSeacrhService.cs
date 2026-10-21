using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace AutoCareApi.Services;

public class ElasticSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly string _defaultIndex;
    public ElasticSearchService(IConfiguration config)
    {
        var uri = config["Elasticsearch:Uri"] ?? throw new InvalidOperationException("Elasticsearch:Uri tidak dikonfigurasi");
        _defaultIndex = config["Elasticsearch:DefaultIndex"] ?? "vehicles";

        var settings = new ElasticsearchClientSettings(new Uri(uri))
            .DefaultIndex(_defaultIndex);

        _client = new ElasticsearchClient(settings);
    }

    public ElasticsearchClient Client => _client;
    public string DefaultIndex => _defaultIndex;
}