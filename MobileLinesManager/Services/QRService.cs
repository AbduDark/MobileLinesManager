
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
        private readonly IImportService _importService;

        public QRService(IImportService importService)
        {
            _importService = importService;
        }

        public async Task<Line> ScanFromWebcamAsync()
        {
            // Note: Full webcam integration requires additional UI component
            // This is a placeholder for the core logic
            await Task.CompletedTask;
            throw new NotImplementedException("يتطلب مسح الكاميرا نافذة واجهة مستخدم مخصصة");
        }

        public async Task<Line> ScanFromImageAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var barcodeReader = new BarcodeReader
                    {
                        AutoRotate = true,
                        TryInverted = true,
                        Options = new DecodingOptions
                        {
                            TryHarder = true,
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

        public Line ParseQrData(string qrData)
        {
            var importResult = _importService.ParseQrPayload(qrData);
            return importResult.SuccessfulLines.FirstOrDefault();
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
