using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MitsubishiApi.Models;

namespace MitsubishiApi.Services;

public class InvoicePdfService
{
    public byte[] GenerateInvoicePdf(Invoice invoice, ServiceHistory history, string customerName, string customerPhone, string? customerEmail, string customerAddress, string? customerCity, string dealerName, string dealerAddress, string dealerPhone, string vehicleModelName, string licensePlate, string serviceTypeName, string? mechanicName)
    {
        // Set license (community = free untuk penggunaan non-komersial)
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                // ===== HEADER =====
                page.Header().Element(ComposeHeader);

                // ===== CONTENT =====
                page.Content().PaddingVertical(20).Column(column =>
                {
                    // Info Customer & Invoice
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("DITAGIHKAN KEPADA").Bold().FontSize(9).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(4).Text(customerName).Bold().FontSize(12);
                            col.Item().Text(customerPhone).FontSize(10);
                            if (!string.IsNullOrEmpty(customerEmail))
                                col.Item().Text(customerEmail).FontSize(10);
                            if (!string.IsNullOrEmpty(customerAddress))
                                col.Item().Text(customerAddress).FontSize(10);
                            if (!string.IsNullOrEmpty(customerCity))
                                col.Item().Text(customerCity).FontSize(10);
                        });

                        row.ConstantItem(30);

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("DETAIL INVOICE").Bold().FontSize(9).FontColor(Colors.Grey.Medium);
                            col.Item().PaddingTop(4).Row(r =>
                            {
                                r.RelativeItem().Text("No. Invoice").FontSize(10);
                                r.ConstantItem(10);
                                r.RelativeItem().Text(invoice.InvoiceNumber).Bold().FontSize(10);
                            });
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Tanggal").FontSize(10);
                                r.ConstantItem(10);
                                r.RelativeItem().Text(invoice.CreatedAt.ToString("dd MMM yyyy")).FontSize(10);
                            });
                            if (invoice.DueDate.HasValue)
                            {
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Jatuh Tempo").FontSize(10);
                                    r.ConstantItem(10);
                                    r.RelativeItem().Text(invoice.DueDate.Value.ToString("dd MMM yyyy")).FontColor(Colors.Red.Medium).FontSize(10);
                                });
                            }
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Status").FontSize(10);
                                r.ConstantItem(10);
                                var color = invoice.PaymentStatus == "Paid" ? Colors.Green.Medium
                                    : invoice.PaymentStatus == "Partial" ? Colors.Orange.Medium
                                    : Colors.Red.Medium;
                                r.RelativeItem().Text(invoice.PaymentStatus).Bold().FontColor(color).FontSize(10);
                            });
                        });
                    });

                    // Info Kendaraan & Servis
                    column.Item().PaddingTop(24).Background(Colors.Grey.Lighten4).Padding(12).Column(col =>
                    {
                        col.Item().Text("INFORMASI KENDARAAN & SERVIS").Bold().FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Kendaraan").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(vehicleModelName + " (" + licensePlate + ")").Bold().FontSize(11);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Tanggal Servis").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(history.ServiceDate.ToString("dd MMM yyyy")).FontSize(11);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("KM Servis").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(history.KmAtService.ToString("N0") + " KM").FontSize(11);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Mekanik").FontSize(9).FontColor(Colors.Grey.Medium);
                                c.Item().Text(mechanicName ?? "-").FontSize(11);
                            });
                        });
                    });

                    // Tabel Item
                    column.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);   // No
                            columns.RelativeColumn(3);    // Deskripsi
                            columns.RelativeColumn(1);    // Qty
                            columns.RelativeColumn(2);    // Harga
                            columns.RelativeColumn(2);    // Subtotal
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#E60012").Padding(8).Text("#").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background("#E60012").Padding(8).Text("Deskripsi").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background("#E60012").Padding(8).AlignRight().Text("Qty").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background("#E60012").Padding(8).AlignRight().Text("Harga").FontColor(Colors.White).Bold().FontSize(10);
                            header.Cell().Background("#E60012").Padding(8).AlignRight().Text("Subtotal").FontColor(Colors.White).Bold().FontSize(10);
                        });

                        // Row 1 - Jasa Servis
                        table.Cell().Padding(8).Text("1").FontSize(10);
                        table.Cell().Padding(8).Text(serviceTypeName).FontSize(10);
                        table.Cell().Padding(8).AlignRight().Text("1").FontSize(10);
                        table.Cell().Padding(8).AlignRight().Text("Rp " + invoice.Subtotal.ToString("N0")).FontSize(10);
                        table.Cell().Padding(8).AlignRight().Text("Rp " + invoice.Subtotal.ToString("N0")).FontSize(10);
                    });

                    // Ringkasan Total
                    column.Item().PaddingTop(16).AlignRight().Width(280).Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal").FontSize(10);
                            row.ConstantItem(140).AlignRight().Text("Rp " + invoice.Subtotal.ToString("N0")).FontSize(10);
                        });

                        if (invoice.DiscountAmount > 0)
                        {
                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Text("Diskon").FontSize(10).FontColor(Colors.Green.Medium);
                                row.ConstantItem(140).AlignRight().Text("- Rp " + invoice.DiscountAmount.ToString("N0")).FontSize(10).FontColor(Colors.Green.Medium);
                            });
                        }

                        col.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text("PPN 11%").FontSize(10);
                            row.ConstantItem(140).AlignRight().Text("Rp " + invoice.TaxAmount.ToString("N0")).FontSize(10);
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(12).FontColor("#E60012");
                            row.ConstantItem(140).AlignRight().Text("Rp " + invoice.TotalAmount.ToString("N0")).Bold().FontSize(12).FontColor("#E60012");
                        });

                        if (invoice.PaidAmount > 0)
                        {
                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Text("Sudah Dibayar").FontSize(10);
                                row.ConstantItem(140).AlignRight().Text("Rp " + invoice.PaidAmount.ToString("N0")).FontSize(10).FontColor(Colors.Green.Medium);
                            });
                            col.Item().PaddingTop(6).Row(row =>
                            {
                                row.RelativeItem().Text("Sisa Tagihan").Bold().FontSize(11);
                                row.ConstantItem(140).AlignRight().Text("Rp " + (invoice.TotalAmount - invoice.PaidAmount).ToString("N0")).Bold().FontSize(11).FontColor(Colors.Red.Medium);
                            });
                        }

                        if (invoice.PaymentStatus == "Paid")
                        {
                            col.Item().PaddingTop(10).Background(Colors.Green.Lighten4).Padding(10).AlignCenter()
                            .Text("LUNAS").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                        }
                    });

                    // Catatan / Rekomendasi
                    if (!string.IsNullOrEmpty(history.Findings) || !string.IsNullOrEmpty(history.Recommendations))
                    {
                        column.Item().PaddingTop(24).Background(Colors.Yellow.Lighten5).Padding(12).Column(col =>
                        {
                            col.Item().Text("CATATAN TEKNISI").Bold().FontSize(9).FontColor(Colors.Orange.Darken2);
                            if (!string.IsNullOrEmpty(history.Findings))
                            {
                                col.Item().PaddingTop(4).Text("Temuan: " + history.Findings).FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(history.Recommendations))
                            {
                                col.Item().PaddingTop(4).Text("Rekomendasi: " + history.Recommendations).FontSize(9);
                            }
                            if (history.NextServiceKm.HasValue && history.NextServiceDate.HasValue)
                            {
                                col.Item().PaddingTop(6).Text($"Servis berikutnya: {history.NextServiceKm.Value:N0} KM atau {history.NextServiceDate.Value:dd MMM yyyy}")
                                    .FontSize(9).Italic().FontColor(Colors.Blue.Darken1);
                            }
                        });
                    }
                });

                // ===== FOOTER =====
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                // Logo diamond Mitsubishi (3 segitiga)
                col.Item().Text("MITSUBISHI").FontSize(22).Bold().FontColor("#E60012").LetterSpacing(0.1f);
                col.Item().Text("Service Booking System").FontSize(9).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(180).AlignRight().Column(col =>
            {
                col.Item().Text("INVOICE").FontSize(28).Bold().FontColor("#E60012");
                col.Item().Text("Official Service Document").FontSize(9).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Column(col =>
        {
            col.Item().AlignCenter().Text("Terima kasih atas kepercayaan Anda kepada Mitsubishi.")
                .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);

            col.Item().PaddingTop(6).AlignCenter().Row(row =>
            {
                row.RelativeItem().AlignCenter().Text("Dokumen ini dicetak secara otomatis dari sistem.")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });

            col.Item().PaddingTop(6).AlignCenter().Text(text =>
            {
                text.Span("Halaman ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                text.Span(" dari ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }
}