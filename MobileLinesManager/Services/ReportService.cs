
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MobileLinesManager.Services
{
    public class ReportService : IReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateCountsReportPdfAsync()
        {
            var operators = await _db.Operators
                .Include(o => o.Categories)
                    .ThenInclude(c => c.Lines)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header()
                        .Text("تقرير إحصائيات الخطوط حسب المشغل والفئة")
                        .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            foreach (var op in operators)
                            {
                                column.Item().PaddingBottom(10).Text(op.Name).SemiBold().FontSize(14);

                                foreach (var cat in op.Categories)
                                {
                                    var total = cat.Lines.Count;
                                    var available = cat.Lines.Count(l => l.Status == "Available");
                                    var assigned = cat.Lines.Count(l => l.Status == "Assigned");

                                    column.Item().Text($"  {cat.Name}: إجمالي {total}, متاح {available}, مخصص {assigned}");
                                }

                                column.Item().PaddingBottom(5);
                            }

                            column.Item().PaddingTop(10).Text($"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateExpiringLinesReportPdfAsync(int daysAhead)
        {
            var expiryDate = DateTime.Today.AddDays(daysAhead);
            
            var lines = await _db.Lines
                .Include(l => l.Category)
                    .ThenInclude(c => c.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.Category.HasExpiry && 
                           l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value <= expiryDate)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Text($"تقرير الخطوط المنتهية/القريبة من الانتهاء ({daysAhead} يوم)")
                        .SemiBold().FontSize(16).FontColor(Colors.Red.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("رقم الهاتف");
                                header.Cell().Element(CellStyle).Text("المشغل");
                                header.Cell().Element(CellStyle).Text("الفئة");
                                header.Cell().Element(CellStyle).Text("المخصص له");
                                header.Cell().Element(CellStyle).Text("تاريخ الانتهاء");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            foreach (var line in lines)
                            {
                                table.Cell().Element(CellStyle).Text(line.PhoneNumber ?? "-");
                                table.Cell().Element(CellStyle).Text(line.Category?.Operator?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(line.Category?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(line.AssignedTo?.FullName ?? "غير مخصص");
                                table.Cell().Element(CellStyle).Text(line.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "-");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ");
                            x.CurrentPageNumber();
                            x.Span(" - تاريخ التقرير: ");
                            x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateWorkerDelaysReportPdfAsync()
        {
            var overdueLines = await _db.Lines
                .Include(l => l.Category)
                    .ThenInclude(c => c.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value < DateTime.Today && 
                           l.Status != "Returned")
                .OrderBy(l => l.AssignedTo.FullName)
                .ThenBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header()
                        .Text("تقرير تأخير العمال عن إرجاع الخطوط")
                        .SemiBold().FontSize(16).FontColor(Colors.Red.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("العامل");
                                header.Cell().Element(CellStyle).Text("رقم الهاتف");
                                header.Cell().Element(CellStyle).Text("المشغل");
                                header.Cell().Element(CellStyle).Text("تاريخ الإرجاع المتوقع");
                                header.Cell().Element(CellStyle).Text("أيام التأخير");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            foreach (var line in overdueLines)
                            {
                                var daysOverdue = (DateTime.Today - line.ExpectedReturnDate.Value).Days;

                                table.Cell().Element(CellStyle).Text(line.AssignedTo?.FullName ?? "غير معروف");
                                table.Cell().Element(CellStyle).Text(line.PhoneNumber ?? "-");
                                table.Cell().Element(CellStyle).Text(line.Category?.Operator?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(line.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "-");
                                table.Cell().Element(CellStyle).Text(daysOverdue.ToString());

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                }
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task ExportCountsToExcelAsync(string filePath)
        {
            var operators = await _db.Operators
                .Include(o => o.Categories)
                    .ThenInclude(c => c.Lines)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("إحصائيات الخطوط");

            // Header
            worksheet.Cell(1, 1).Value = "المشغل";
            worksheet.Cell(1, 2).Value = "الفئة";
            worksheet.Cell(1, 3).Value = "الإجمالي";
            worksheet.Cell(1, 4).Value = "متاح";
            worksheet.Cell(1, 5).Value = "مخصص";
            worksheet.Cell(1, 6).Value = "محجوب";

            worksheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

            int row = 2;
            foreach (var op in operators)
            {
                foreach (var cat in op.Categories)
                {
                    worksheet.Cell(row, 1).Value = op.Name;
                    worksheet.Cell(row, 2).Value = cat.Name;
                    worksheet.Cell(row, 3).Value = cat.Lines.Count;
                    worksheet.Cell(row, 4).Value = cat.Lines.Count(l => l.Status == "Available");
                    worksheet.Cell(row, 5).Value = cat.Lines.Count(l => l.Status == "Assigned");
                    worksheet.Cell(row, 6).Value = cat.Lines.Count(l => l.Status == "Blocked");
                    row++;
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public async Task ExportExpiringLinesToExcelAsync(string filePath, int daysAhead)
        {
            var expiryDate = DateTime.Today.AddDays(daysAhead);
            
            var lines = await _db.Lines
                .Include(l => l.Category)
                    .ThenInclude(c => c.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.Category.HasExpiry && 
                           l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value <= expiryDate)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الخطوط المنتهية");

            // Header
            worksheet.Cell(1, 1).Value = "رقم الهاتف";
            worksheet.Cell(1, 2).Value = "المشغل";
            worksheet.Cell(1, 3).Value = "الفئة";
            worksheet.Cell(1, 4).Value = "المخصص له";
            worksheet.Cell(1, 5).Value = "تاريخ الانتهاء";
            worksheet.Cell(1, 6).Value = "الحالة";

            worksheet.Range(1, 1, 1, 6).Style.Font.Bold = true;

            int row = 2;
            foreach (var line in lines)
            {
                worksheet.Cell(row, 1).Value = line.PhoneNumber ?? "-";
                worksheet.Cell(row, 2).Value = line.Category?.Operator?.Name ?? "-";
                worksheet.Cell(row, 3).Value = line.Category?.Name ?? "-";
                worksheet.Cell(row, 4).Value = line.AssignedTo?.FullName ?? "غير مخصص";
                worksheet.Cell(row, 5).Value = line.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "-";
                worksheet.Cell(row, 6).Value = line.Status ?? "-";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }
    }
}
