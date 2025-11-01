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

        public async Task<ImportResult> ImportFromCSVAsync(string filePath, int defaultGroupId)
        {
            var result = new ImportResult();

            try
            {
                var group = await _db.Groups
                    .Include(g => g.Operator)
                    .Include(g => g.Lines)
                    .FirstOrDefaultAsync(g => g.Id == defaultGroupId);

                if (group == null)
                {
                    result.Errors.Add("المجموعة المحددة غير موجودة");
                    return result;
                }

                if (group.IsFull)
                {
                    result.Errors.Add($"المجموعة '{group.Name}' ممتلئة (الحد الأقصى {group.MaxLinesCount} خط)");
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

                        if (group.CurrentLinesCount >= group.MaxLinesCount)
                        {
                            result.Errors.Add($"المجموعة وصلت للحد الأقصى ({group.MaxLinesCount} خط)");
                            break;
                        }

                        var exists = await _db.Lines.AnyAsync(l => l.PhoneNumber == record.PhoneNumber);
                        if (exists)
                        {
                            result.Errors.Add($"الرقم {record.PhoneNumber} موجود بالفعل");
                            continue;
                        }

                        var line = new Line
                        {
                            GroupId = defaultGroupId,
                            PhoneNumber = record.PhoneNumber,
                            SerialNumber = record.SerialNumber ?? string.Empty,
                            AssociatedName = record.AssociatedName ?? string.Empty,
                            NationalId = record.NationalId ?? string.Empty,
                            CashWalletId = record.CashWalletId ?? string.Empty,
                            Status = "Available",
                            Notes = record.Notes ?? string.Empty,
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

        public async Task<ImportResult> ImportFromQRDataAsync(string qrData, int defaultGroupId)
        {
            var result = new ImportResult();

            try
            {
                var parts = qrData.Split('|');

                if (parts.Length < 1)
                {
                    result.Errors.Add("صيغة QR غير صحيحة");
                    return result;
                }

                var phoneNumber = parts[0];

                var exists = await _db.Lines.AnyAsync(l => l.PhoneNumber == phoneNumber);
                if (exists)
                {
                    result.Errors.Add($"الرقم {phoneNumber} موجود بالفعل");
                    return result;
                }

                var group = await _db.Groups
                    .Include(g => g.Operator)
                    .Include(g => g.Lines)
                    .FirstOrDefaultAsync(g => g.Id == defaultGroupId);

                if (group == null)
                {
                    result.Errors.Add("المجموعة المحددة غير موجودة");
                    return result;
                }

                if (group.IsFull)
                {
                    result.Errors.Add($"المجموعة '{group.Name}' ممتلئة");
                    return result;
                }

                var line = new Line
                {
                    GroupId = defaultGroupId,
                    PhoneNumber = phoneNumber,
                    SerialNumber = parts.Length > 1 ? parts[1] : string.Empty,
                    AssociatedName = parts.Length > 2 ? parts[2] : string.Empty,
                    NationalId = parts.Length > 3 ? parts[3] : string.Empty,
                    CashWalletId = parts.Length > 4 ? parts[4] : string.Empty,
                    Status = "Available",
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
                var parts = payload.Split('|');

                if (parts.Length < 1)
                {
                    result.Errors.Add("صيغة QR غير صحيحة");
                    return result;
                }

                var line = new Line
                {
                    PhoneNumber = parts[0],
                    SerialNumber = parts.Length > 1 ? parts[1] : string.Empty,
                    AssociatedName = parts.Length > 2 ? parts[2] : string.Empty,
                    NationalId = parts.Length > 3 ? parts[3] : string.Empty,
                    CashWalletId = parts.Length > 4 ? parts[4] : string.Empty,
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
            public string PhoneNumber { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
            public string AssociatedName { get; set; } = string.Empty;
            public string NationalId { get; set; } = string.Empty;
            public string CashWalletId { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }
    }
}
