
using System.Collections.Generic;
using System.Threading.Tasks;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public interface IImportService
    {
        Task<ImportResult> ImportFromCsvAsync(string filePath, int defaultCategoryId);
        ImportResult ParseQrPayload(string payload);
    }

    public class ImportResult
    {
        public List<Line> SuccessfulLines { get; set; } = new List<Line>();
        public List<string> Errors { get; set; } = new List<string>();
        public int TotalProcessed { get; set; }
    }
}
