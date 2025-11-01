using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ClosedXML.Excel;

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
                .Include(o => o.Groups)
                .ThenInclude(g => g.Lines)
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
                        .Text("تقرير إحصائيات الخطوط")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            foreach (var op in operators)
                            {
                                column.Item().Text($"{op.Name}").SemiBold().FontSize(14);
                                
                                foreach (var grp in op.Groups)
                                {
                                    var total = grp.Lines?.Count ?? 0;
                                    var available = grp.Lines?.Count(l => l.Status == "Available") ?? 0;
                                    var assigned = grp.Lines?.Count(l => l.Status == "Assigned") ?? 0;

                                    column.Item().Text($"  {grp.Name}: إجمالي {total}, متاح {available}, مخصص {assigned}");
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
                .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
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
                                header.Cell().Element(CellStyle).Text("المجموعة");
                                header.Cell().Element(CellStyle).Text("المخصص له");
                                header.Cell().Element(CellStyle).Text("تاريخ الانتهاء");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            foreach (var line in lines)
                            {
                                table.Cell().Element(CellStyle).Text(line.PhoneNumber);
                                table.Cell().Element(CellStyle).Text(line.Group?.Operator?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(line.Group?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(line.AssignedTo?.Username ?? "-");
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
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateAssignmentsReportPdfAsync(DateTime startDate, DateTime endDate)
        {
            var assignments = await _db.AssignmentLogs
                .Include(a => a.Line)
                    .ThenInclude(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(a => a.ToUser)
                .Where(a => a.AssignedAt >= startDate && a.AssignedAt <= endDate)
                .OrderByDescending(a => a.AssignedAt)
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
                        .Text($"تقرير التسليمات ({startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd})")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

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
                                header.Cell().Element(CellStyle).Text("المستلم");
                                header.Cell().Element(CellStyle).Text("تاريخ التسليم");
                                header.Cell().Element(CellStyle).Text("الحالة");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            foreach (var assignment in assignments)
                            {
                                table.Cell().Element(CellStyle).Text(assignment.Line?.PhoneNumber ?? "-");
                                table.Cell().Element(CellStyle).Text(assignment.Line?.Group?.Operator?.Name ?? "-");
                                table.Cell().Element(CellStyle).Text(assignment.ToUser?.Username ?? "-");
                                table.Cell().Element(CellStyle).Text(assignment.AssignedAt.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text(assignment.Status);

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

        public async Task ExportLinesToExcelAsync(List<Line> lines, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الخطوط");

            worksheet.Cell(1, 1).Value = "رقم الهاتف";
            worksheet.Cell(1, 2).Value = "المشغل";
            worksheet.Cell(1, 3).Value = "المجموعة";
            worksheet.Cell(1, 4).Value = "الرقم التسلسلي";
            worksheet.Cell(1, 5).Value = "الحالة";
            worksheet.Cell(1, 6).Value = "المخصص له";
            worksheet.Cell(1, 7).Value = "ملاحظات";

            var row = 2;
            foreach (var line in lines)
            {
                worksheet.Cell(row, 1).Value = line.PhoneNumber;
                worksheet.Cell(row, 2).Value = line.Group?.Operator?.Name ?? "-";
                worksheet.Cell(row, 3).Value = line.Group?.Name ?? "-";
                worksheet.Cell(row, 4).Value = line.SerialNumber;
                worksheet.Cell(row, 5).Value = line.Status;
                worksheet.Cell(row, 6).Value = line.AssignedTo?.Username ?? "-";
                worksheet.Cell(row, 7).Value = line.Notes;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public async Task<string> GenerateSummaryReportAsync()
        {
            var operators = await _db.Operators
                .Include(o => o.Groups)
                .ThenInclude(g => g.Lines)
                .ToListAsync();

            var report = "تقرير ملخص إدارة الخطوط\n";
            report += $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            foreach (var op in operators)
            {
                report += $"{op.Name}:\n";
                
                foreach (var grp in op.Groups)
                {
                    var total = grp.Lines?.Count ?? 0;
                    var available = grp.Lines?.Count(l => l.Status == "Available") ?? 0;
                    var assigned = grp.Lines?.Count(l => l.Status == "Assigned") ?? 0;

                    report += $"  {grp.Name}: إجمالي {total}, متاح {available}, مخصص {assigned}\n";
                }
                
                report += "\n";
            }

            return report;
        }

        public async Task<string> GenerateExpiringLinesReportAsync(int daysAhead)
        {
            var expiryDate = DateTime.Today.AddDays(daysAhead);
            
            var lines = await _db.Lines
                .Include(l => l.Group)
                .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value <= expiryDate)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            var report = $"تقرير الخطوط المنتهية/القريبة من الانتهاء ({daysAhead} يوم)\n";
            report += $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            foreach (var line in lines)
            {
                report += $"رقم الهاتف: {line.PhoneNumber}\n";
                report += $"المشغل: {line.Group?.Operator?.Name}\n";
                report += $"المجموعة: {line.Group?.Name}\n";
                report += $"المخصص له: {line.AssignedTo?.Username ?? "-"}\n";
                report += $"تاريخ الانتهاء: {line.ExpectedReturnDate?.ToString("yyyy-MM-dd")}\n";
                report += "---\n";
            }

            return report;
        }

        public async Task<string> GenerateAssignmentHistoryReportAsync(DateTime startDate, DateTime endDate)
        {
            var assignments = await _db.AssignmentLogs
                .Include(a => a.Line)
                    .ThenInclude(l => l.Group)
                .Include(a => a.ToUser)
                .Where(a => a.AssignedAt >= startDate && a.AssignedAt <= endDate)
                .OrderByDescending(a => a.AssignedAt)
                .ToListAsync();

            var report = $"تقرير التسليمات ({startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd})\n";
            report += $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            foreach (var assignment in assignments)
            {
                report += $"رقم الهاتف: {assignment.Line?.PhoneNumber}\n";
                report += $"المشغل: {assignment.Line?.Group?.Operator?.Name}\n";
                report += $"المستلم: {assignment.ToUser?.Username}\n";
                report += $"تاريخ التسليم: {assignment.AssignedAt:yyyy-MM-dd}\n";
                report += $"الحالة: {assignment.Status}\n";
                report += "---\n";
            }

            return report;
        }

        public async Task<byte[]> GenerateWorkerDelaysReportPdfAsync()
        {
            var overdueLines = await _db.Lines
                .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value < DateTime.Today &&
                           l.Status == "Assigned")
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
                        .Text("تقرير التأخيرات")
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
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("رقم الهاتف");
                                header.Cell().Element(CellStyle).Text("المستلم");
                                header.Cell().Element(CellStyle).Text("الموعد المتوقع");
                                header.Cell().Element(CellStyle).Text("أيام التأخير");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.BorderBottom(1).BorderColor(Colors.Black).PaddingVertical(5);
                                }
                            });

                            foreach (var line in overdueLines)
                            {
                                var daysOverdue = (DateTime.Today - line.ExpectedReturnDate.Value).Days;
                                table.Cell().Element(CellStyle).Text(line.PhoneNumber);
                                table.Cell().Element(CellStyle).Text(line.AssignedTo?.Username ?? "-");
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
                .Include(o => o.Groups)
                .ThenInclude(g => g.Lines)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("إحصائيات");

            worksheet.Cell(1, 1).Value = "المشغل";
            worksheet.Cell(1, 2).Value = "المجموعة";
            worksheet.Cell(1, 3).Value = "إجمالي";
            worksheet.Cell(1, 4).Value = "متاح";
            worksheet.Cell(1, 5).Value = "مخصص";

            var row = 2;
            foreach (var op in operators)
            {
                foreach (var grp in op.Groups)
                {
                    var total = grp.Lines?.Count ?? 0;
                    var available = grp.Lines?.Count(l => l.Status == "Available") ?? 0;
                    var assigned = grp.Lines?.Count(l => l.Status == "Assigned") ?? 0;

                    worksheet.Cell(row, 1).Value = op.Name;
                    worksheet.Cell(row, 2).Value = grp.Name;
                    worksheet.Cell(row, 3).Value = total;
                    worksheet.Cell(row, 4).Value = available;
                    worksheet.Cell(row, 5).Value = assigned;
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
                .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value <= expiryDate)
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الخطوط المنتهية");

            worksheet.Cell(1, 1).Value = "رقم الهاتف";
            worksheet.Cell(1, 2).Value = "المشغل";
            worksheet.Cell(1, 3).Value = "المجموعة";
            worksheet.Cell(1, 4).Value = "المخصص له";
            worksheet.Cell(1, 5).Value = "تاريخ الانتهاء";

            var row = 2;
            foreach (var line in lines)
            {
                worksheet.Cell(row, 1).Value = line.PhoneNumber;
                worksheet.Cell(row, 2).Value = line.Group?.Operator?.Name ?? "-";
                worksheet.Cell(row, 3).Value = line.Group?.Name ?? "-";
                worksheet.Cell(row, 4).Value = line.AssignedTo?.Username ?? "-";
                worksheet.Cell(row, 5).Value = line.ExpectedReturnDate?.ToString("yyyy-MM-dd") ?? "-";
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public async Task ExportLinesToExcelAsync(IEnumerable<Line> lines, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الخطوط");

            worksheet.Cell(1, 1).Value = "رقم الهاتف";
            worksheet.Cell(1, 2).Value = "المشغل";
            worksheet.Cell(1, 3).Value = "المجموعة";
            worksheet.Cell(1, 4).Value = "الرقم التسلسلي";
            worksheet.Cell(1, 5).Value = "الحالة";
            worksheet.Cell(1, 6).Value = "المخصص له";
            worksheet.Cell(1, 7).Value = "ملاحظات";

            var row = 2;
            foreach (var line in lines)
            {
                worksheet.Cell(row, 1).Value = line.PhoneNumber;
                worksheet.Cell(row, 2).Value = line.Group?.Operator?.Name ?? "-";
                worksheet.Cell(row, 3).Value = line.Group?.Name ?? "-";
                worksheet.Cell(row, 4).Value = line.SerialNumber;
                worksheet.Cell(row, 5).Value = line.Status;
                worksheet.Cell(row, 6).Value = line.AssignedTo?.Username ?? "-";
                worksheet.Cell(row, 7).Value = line.Notes;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public async Task<string> GenerateCountByOperatorAndCategoryReportAsync()
        {
            var operators = await _db.Operators
                .Include(o => o.Groups)
                .ThenInclude(g => g.Lines)
                .ToListAsync();

            var report = "تقرير إحصائيات الخطوط حسب المشغل والمجموعة\n";
            report += $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            foreach (var op in operators)
            {
                report += $"{op.Name}:\n";
                
                foreach (var grp in op.Groups)
                {
                    var total = grp.Lines?.Count ?? 0;
                    var available = grp.Lines?.Count(l => l.Status == "Available") ?? 0;
                    var assigned = grp.Lines?.Count(l => l.Status == "Assigned") ?? 0;

                    report += $"  {grp.Name}: إجمالي {total}, متاح {available}, مخصص {assigned}\n";
                }
                
                report += "\n";
            }

            return report;
        }

        public async Task<string> GenerateWorkerDelayReportAsync()
        {
            var overdueLines = await _db.Lines
                .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue && 
                           l.ExpectedReturnDate.Value < DateTime.Today &&
                           l.Status == "Assigned")
                .OrderBy(l => l.ExpectedReturnDate)
                .ToListAsync();

            var report = "تقرير التأخيرات\n";
            report += $"تاريخ التقرير: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";

            foreach (var line in overdueLines)
            {
                var daysOverdue = (DateTime.Today - line.ExpectedReturnDate.Value).Days;
                report += $"رقم الهاتف: {line.PhoneNumber}\n";
                report += $"المستلم: {line.AssignedTo?.Username ?? "-"}\n";
                report += $"الموعد المتوقع: {line.ExpectedReturnDate?.ToString("yyyy-MM-dd")}\n";
                report += $"أيام التأخير: {daysOverdue}\n";
                report += "---\n";
            }

            return report;
        }

        public async Task ExportReportToPdfAsync(string reportContent, string filePath)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

                    page.Header()
                        .Text("تقرير")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Text(reportContent);

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("صفحة ");
                            x.CurrentPageNumber();
                        });
                });
            });

            var pdfBytes = document.GeneratePdf();
            await System.IO.File.WriteAllBytesAsync(filePath, pdfBytes);
        }
    }
}
