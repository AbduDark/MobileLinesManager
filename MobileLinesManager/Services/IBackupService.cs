
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MobileLinesManager.Services
{
    public interface IBackupService
    {
        Task<string> BackupDatabaseAsync();
        Task RestoreDatabaseAsync(string backupFilePath);
        List<string> GetAvailableBackups();
    }
}
