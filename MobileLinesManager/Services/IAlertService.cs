
using System.Collections.Generic;
using System.Threading.Tasks;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public interface IAlertService
    {
        Task<List<AlertItem>> CheckExpiryAlertsAsync();
        Task<List<AlertItem>> CheckOverdueAssignmentsAsync();
        Task<List<AlertItem>> CheckGroupValidityAlertsAsync();
        Task<List<AlertItem>> CheckGroupDeliveryAlertsAsync();
        Task<List<AlertItem>> CheckAllAlertsAsync();
        void StartPeriodicCheck(int intervalMinutes = 30);
        void StopPeriodicCheck();
    }

    public class AlertItem
    {
        public Line Line { get; set; }
        public string Message { get; set; }
        public string AlertType { get; set; }
        public DateTime AlertDate { get; set; } = DateTime.Now;
    }
}
