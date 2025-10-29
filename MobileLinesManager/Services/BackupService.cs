
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MobileLinesManager.Services
{
    public class BackupService : IBackupService
    {
        private readonly string _dbPath;
        private readonly string _backupFolder;

        public BackupService()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MobileLinesManager"
            );
            
            _dbPath = Path.Combine(appDataPath, "mobile_lines.db");
            _backupFolder = Path.Combine(appDataPath, "Backups");
            
            Directory.CreateDirectory(_backupFolder);
        }

        public async Task<string> BackupDatabaseAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(_dbPath))
                    {
                        throw new FileNotFoundException("ملف قاعدة البيانات غير موجود");
                    }

                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var backupFileName = $"mobile_lines_backup_{timestamp}.db";
                    var backupPath = Path.Combine(_backupFolder, backupFileName);

                    // Use SQLite backup API for safe backup while database is in use
                    using var sourceConnection = new SqliteConnection($"Data Source={_dbPath}");
                    sourceConnection.Open();

                    using var destinationConnection = new SqliteConnection($"Data Source={backupPath}");
                    destinationConnection.Open();

                    sourceConnection.BackupDatabase(destinationConnection);

                    return backupPath;
                }
                catch (Exception ex)
                {
                    throw new Exception($"فشل إنشاء النسخة الاحتياطية: {ex.Message}");
                }
            });
        }

        public async Task RestoreDatabaseAsync(string backupFilePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(backupFilePath))
                    {
                        throw new FileNotFoundException("ملف النسخة الاحتياطية غير موجود");
                    }

                    // Create a safety backup before restoring
                    var safetyBackupPath = Path.Combine(_backupFolder, $"safety_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    if (File.Exists(_dbPath))
                    {
                        File.Copy(_dbPath, safetyBackupPath, true);
                    }

                    try
                    {
                        // Close all connections and replace the database file
                        SqliteConnection.ClearAllPools();
                        
                        if (File.Exists(_dbPath))
                        {
                            File.Delete(_dbPath);
                        }

                        File.Copy(backupFilePath, _dbPath, true);
                    }
                    catch (Exception)
                    {
                        // Restore from safety backup if restore failed
                        if (File.Exists(safetyBackupPath))
                        {
                            File.Copy(safetyBackupPath, _dbPath, true);
                        }
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"فشل استعادة النسخة الاحتياطية: {ex.Message}");
                }
            });
        }

        public List<string> GetAvailableBackups()
        {
            try
            {
                if (!Directory.Exists(_backupFolder))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(_backupFolder, "*.db")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}
