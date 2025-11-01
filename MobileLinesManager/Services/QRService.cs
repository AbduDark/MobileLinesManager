
using System;
using System.Drawing;
using System.IO;
using System.Linq;
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
            // Note: Full webcam integration requires additional UI component
            // This is a placeholder for the core logic
            await Task.CompletedTask;
            throw new NotImplementedException("يتطلب مسح الكاميرا نافذة واجهة مستخدم مخصصة");
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
                // Expected format: PhoneNumber|SerialNumber|CategoryId|WalletId
                var parts = qrData.Split('|');

                if (parts.Length < 1)
                {
                    return null;
                }

                var line = new Line
                {
                    PhoneNumber = parts[0],
                    SerialNumber = parts.Length > 1 ? parts[1] : string.Empty,
                    CategoryId = parts.Length > 2 && int.TryParse(parts[2], out var catId) ? catId : 0,
                    WalletId = parts.Length > 3 ? parts[3] : string.Empty,
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
                // Format: PhoneNumber|SerialNumber|CategoryId|WalletId
                var payload = $"{line.PhoneNumber}|{line.SerialNumber}|{line.CategoryId}|{line.WalletId}";

                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new EncodingOptions
                    {
                        Width = 300,
                        Height = 300,
                        Margin = 1
                    }
                };

                using var bitmap = writer.Write(payload);
                using var ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
