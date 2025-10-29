
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public class ImportService : IImportService
    {
        private readonly AppDbContext _db;

        public ImportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ImportResult> ImportFromCSVAsync(string filePath, int defaultCategoryId)
        {
            var result = new ImportResult();

            try
            {
                var category = await _db.Categories
                    .Include(c => c.Operator)
                    .FirstOrDefaultAsync(c => c.Id == defaultCategoryId);

                if (category == null)
                {
                    result.Errors.Add("الفئة المحددة غير موجودة");
                    return result;
                }

                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, config);

                var records = csv.GetRecords<CsvLineRecord>().ToList();
                result.TotalProcessed = records.Count;

                foreach (var record in records)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(record.PhoneNumber))
                        {
                            result.Errors.Add($"رقم الهاتف فارغ في السطر");
                            continue;
                        }

                        // Check if line already exists
                        var exists = await _db.Lines.AnyAsync(l => l.PhoneNumber == record.PhoneNumber);
                        if (exists)
                        {
                            result.Errors.Add($"الرقم {record.PhoneNumber} موجود بالفعل");
                            continue;
                        }

                        var line = new Line
                        {
                            CategoryId = defaultCategoryId,
                            PhoneNumber = record.PhoneNumber,
                            SerialNumber = record.SerialNumber,
                            Status = "Available",
                            HasWallet = category.RequiresWallet,
                            WalletId = record.WalletId,
                            Notes = record.Notes,
                            CreatedAt = DateTime.Now
                        };

                        _db.Lines.Add(line);
                        result.SuccessfulLines.Add(line);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"خطأ في معالجة الرقم {record.PhoneNumber}: {ex.Message}");
                    }
                }

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خطأ في قراءة الملف: {ex.Message}");
            }

            return result;
        }

        public async Task<ImportResult> ImportFromQRDataAsync(string qrData, int defaultCategoryId)
        {
            var result = new ImportResult();

            try
            {
                // Expected format: PhoneNumber|SerialNumber|CategoryId|WalletId
                var parts = qrData.Split('|');

                if (parts.Length < 1)
                {
                    result.Errors.Add("صيغة QR غير صحيحة");
                    return result;
                }

                var phoneNumber = parts[0];

                // Check if line already exists
                var exists = await _db.Lines.AnyAsync(l => l.PhoneNumber == phoneNumber);
                if (exists)
                {
                    result.Errors.Add($"الرقم {phoneNumber} موجود بالفعل");
                    return result;
                }

                var category = await _db.Categories
                    .Include(c => c.Operator)
                    .FirstOrDefaultAsync(c => c.Id == defaultCategoryId);

                if (category == null)
                {
                    result.Errors.Add("الفئة المحددة غير موجودة");
                    return result;
                }

                var line = new Line
                {
                    PhoneNumber = phoneNumber,
                    SerialNumber = parts.Length > 1 ? parts[1] : null,
                    CategoryId = defaultCategoryId,
                    WalletId = parts.Length > 3 ? parts[3] : null,
                    Status = "Available",
                    HasWallet = category.RequiresWallet,
                    CreatedAt = DateTime.Now
                };

                _db.Lines.Add(line);
                await _db.SaveChangesAsync();

                result.SuccessfulLines.Add(line);
                result.TotalProcessed = 1;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خطأ في استيراد بيانات QR: {ex.Message}");
            }

            return result;
        }

        public ImportResult ParseQrPayload(string payload)
        {
            var result = new ImportResult();

            try
            {
                // Expected format: PhoneNumber|SerialNumber|CategoryId|WalletId
                var parts = payload.Split('|');

                if (parts.Length < 1)
                {
                    result.Errors.Add("صيغة QR غير صحيحة");
                    return result;
                }

                var line = new Line
                {
                    PhoneNumber = parts[0],
                    SerialNumber = parts.Length > 1 ? parts[1] : null,
                    CategoryId = parts.Length > 2 && int.TryParse(parts[2], out var catId) ? catId : 0,
                    WalletId = parts.Length > 3 ? parts[3] : null,
                    Status = "Available",
                    CreatedAt = DateTime.Now
                };

                result.SuccessfulLines.Add(line);
                result.TotalProcessed = 1;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"خطأ في تحليل بيانات QR: {ex.Message}");
            }

            return result;
        }

        private class CsvLineRecord
        {
            public string PhoneNumber { get; set; }
            public string SerialNumber { get; set; }
            public string WalletId { get; set; }
            public string Notes { get; set; }
        }
    }
}
