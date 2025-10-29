
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public interface IReportService
    {
        // PDF Reports
        Task<byte[]> GenerateCountsReportPdfAsync();
        Task<byte[]> GenerateExpiringLinesReportPdfAsync(int daysAhead);
        Task<byte[]> GenerateWorkerDelaysReportPdfAsync();
        
        // Excel Exports
        Task ExportCountsToExcelAsync(string filePath);
        Task ExportExpiringLinesToExcelAsync(string filePath, int daysAhead);
        Task ExportLinesToExcelAsync(IEnumerable<Line> lines, string filePath);
        
        // Text Reports (for in-app display)
        Task<string> GenerateCountByOperatorAndCategoryReportAsync();
        Task<string> GenerateExpiringLinesReportAsync(int daysAhead);
        Task<string> GenerateWorkerDelayReportAsync();
        Task<string> GenerateAssignmentHistoryReportAsync(DateTime startDate, DateTime endDate);
        
        // Export text reports
        Task ExportReportToPdfAsync(string reportText, string filePath);
    }
}
