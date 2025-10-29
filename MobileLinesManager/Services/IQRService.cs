
using System.Threading.Tasks;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public interface IQRService
    {
        Task<Line> ScanFromWebcamAsync();
        Task<Line> ScanFromImageAsync(string imagePath);
        Line ParseQrData(string qrData);
        byte[] GenerateQrCode(Line line);
    }
}
