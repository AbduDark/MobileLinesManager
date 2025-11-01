
using System.Collections.Generic;
using System.Threading.Tasks;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public interface IImportService
    {
        Task<ImportResult> ImportFromCSVAsync(string filePath, int defaultGroupId);
        Task<ImportResult> ImportFromQRDataAsync(string qrData, int defaultGroupId);
        ImportResult ParseQrPayload(string payload);
    }

    public class ImportResult
    {
        public List<Line> SuccessfulLines { get; set; } = new List<Line>();
        public List<string> Errors { get; set; } = new List<string>();
        public int TotalProcessed { get; set; }
        public int SuccessCount => SuccessfulLines.Count;
    }
}
