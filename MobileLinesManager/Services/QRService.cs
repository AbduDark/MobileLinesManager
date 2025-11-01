using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using MobileLinesManager.Models;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace MobileLinesManager.Services
{
    public class QRService : IQRService
    {
        public QRService()
        {
        }

        public async Task<Line> ScanFromWebcamAsync()
        {
            return await Task.Run<Line>(() =>
            {
                // This will be called from QRScannerWindow
                // The actual implementation is in the UI layer
                throw new NotImplementedException("استخدم QRScannerWindow للمسح من الكاميرا");
            });
        }

        public async Task<Line?> ScanFromImageAsync(string imagePath)
        {
            return await Task.Run<Line?>(() =>
            {
                try
                {
                    var barcodeReader = new BarcodeReader
                    {
                        AutoRotate = true,
                        Options = new DecodingOptions
                        {
                            TryHarder = true,
                            TryInverted = true,
                            PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                        }
                    };

                    using var bitmap = (Bitmap)Image.FromFile(imagePath);
                    var result = barcodeReader.Decode(bitmap);

                    if (result != null)
                    {
                        return ParseQrData(result.Text);
                    }

                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public async Task<string?> ScanQRFromFileAsync(string imagePath)
        {
            return await Task.Run<string?>(() =>
            {
                try
                {
                    var barcodeReader = new BarcodeReader
                    {
                        AutoRotate = true,
                        Options = new DecodingOptions
                        {
                            TryHarder = true,
                            TryInverted = true,
                            PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                        }
                    };

                    using var bitmap = (Bitmap)Image.FromFile(imagePath);
                    var result = barcodeReader.Decode(bitmap);

                    return result?.Text;
                }
                catch (Exception)
                {
                    return null;
                }
            });
        }

        public Line? ParseQrData(string qrData)
        {
            try
            {
                // Expected format: PhoneNumber|SerialNumber|GroupId|CashWalletId|AssociatedName|NationalId
                var parts = qrData.Split('|');

                if (parts.Length < 1)
                {
                    return null;
                }

                var line = new Line
                {
                    PhoneNumber = parts[0],
                    SerialNumber = parts.Length > 1 ? parts[1] : string.Empty,
                    GroupId = parts.Length > 2 && int.TryParse(parts[2], out var grpId) ? grpId : 0,
                    CashWalletId = parts.Length > 3 ? parts[3] : string.Empty,
                    AssociatedName = parts.Length > 4 ? parts[4] : string.Empty,
                    NationalId = parts.Length > 5 ? parts[5] : string.Empty,
                    Status = "Available",
                    CreatedAt = DateTime.Now
                };

                return line;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public byte[] GenerateQrCode(Line line)
        {
            try
            {
                // Format: PhoneNumber|SerialNumber|GroupId|CashWalletId|AssociatedName|NationalId
                var payload = $"{line.PhoneNumber}|{line.SerialNumber}|{line.GroupId}|{line.CashWalletId}|{line.AssociatedName}|{line.NationalId}";

                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 2
                    }
                };

                using var bitmap = writer.Write(payload);
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<byte>();
            }
        }
    }
}