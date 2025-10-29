
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileLinesManager.Services
{
    public interface IReportService
    {
        Task<byte[]> GenerateCountsReportPdfAsync();
        Task<byte[]> GenerateExpiringLinesReportPdfAsync(int daysAhead);
        Task<byte[]> GenerateWorkerDelaysReportPdfAsync();
        Task ExportCountsToExcelAsync(string filePath);
        Task ExportExpiringLinesToExcelAsync(string filePath, int daysAhead);
    }
}
