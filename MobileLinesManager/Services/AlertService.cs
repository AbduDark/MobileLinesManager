
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.Models;

namespace MobileLinesManager.Services
{
    public class AlertService : IAlertService
    {
        private System.Timers.Timer _timer;
        private readonly IServiceProvider _serviceProvider;

        public AlertService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<List<AlertItem>> CheckExpiryAlertsAsync()
        {
            // دمج مع وظيفة CheckGroupValidityAlertsAsync للنظام الجديد
            return await CheckGroupValidityAlertsAsync();
        }

        public async Task<List<AlertItem>> CheckOverdueAssignmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var items = new List<AlertItem>();
            var overdueLines = await db.Lines
                .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.ExpectedReturnDate.HasValue 
                    && l.ExpectedReturnDate.Value < DateTime.Today 
                    && l.Status != "Returned")
                .ToListAsync();

            foreach (var line in overdueLines)
            {
                var daysOverdue = (DateTime.Today - line.ExpectedReturnDate.Value).Days;
                items.Add(new AlertItem
                {
                    Line = line,
                    Message = $"خط {line.PhoneNumber} متأخر عن الموعد المحدد {line.ExpectedReturnDate.Value:d} ({daysOverdue} يوم تأخير)",
                    AlertType = "NotReturned"
                });
            }

            return items;
        }

        public async Task<List<AlertItem>> CheckGroupValidityAlertsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var items = new List<AlertItem>();
            var groupsWithCashWallet = await db.Groups
                .Include(g => g.Operator)
                .Where(g => g.Type == GroupType.WithCashWallet && g.ValidityDate.HasValue)
                .ToListAsync();

            foreach (var group in groupsWithCashWallet)
            {
                var daysUntilExpiry = (group.ValidityDate.Value - DateTime.Today).TotalDays;

                if (daysUntilExpiry <= group.AlertDaysBeforeExpiry && daysUntilExpiry >= 0)
                {
                    items.Add(new AlertItem
                    {
                        Line = null,
                        Message = $"مجموعة '{group.Name}' ({group.Operator.Name}) صلاحيتها تنتهي في {group.ValidityDate.Value:yyyy-MM-dd} (بعد {(int)daysUntilExpiry} يوم)",
                        AlertType = "GroupValidityExpiring"
                    });
                }
                else if (daysUntilExpiry < 0)
                {
                    items.Add(new AlertItem
                    {
                        Line = null,
                        Message = $"مجموعة '{group.Name}' ({group.Operator.Name}) منتهية الصلاحية منذ {group.ValidityDate.Value:yyyy-MM-dd}",
                        AlertType = "GroupValidityExpired"
                    });
                }
            }

            return items;
        }

        public async Task<List<AlertItem>> CheckGroupDeliveryAlertsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var items = new List<AlertItem>();
            var overdueGroups = await db.Groups
                .Include(g => g.Operator)
                .Where(g => g.Status == GroupStatus.DeliveredToClient 
                    && g.ExpectedReturnDate.HasValue 
                    && g.ExpectedReturnDate.Value < DateTime.Today)
                .ToListAsync();

            foreach (var group in overdueGroups)
            {
                var daysOverdue = (DateTime.Today - group.ExpectedReturnDate.Value).Days;
                items.Add(new AlertItem
                {
                    Line = null,
                    Message = $"مجموعة '{group.Name}' ({group.Operator.Name}) مسلمة للعميل '{group.DeliveredToClientName}' ولم تستلم بعد. متأخرة {daysOverdue} يوم عن الموعد المحدد {group.ExpectedReturnDate.Value:yyyy-MM-dd}",
                    AlertType = "GroupNotReturned"
                });
            }

            return items;
        }

        public async Task<List<AlertItem>> CheckAllAlertsAsync()
        {
            var expiryAlerts = await CheckExpiryAlertsAsync();
            var overdueAlerts = await CheckOverdueAssignmentsAsync();
            var groupValidityAlerts = await CheckGroupValidityAlertsAsync();
            var groupDeliveryAlerts = await CheckGroupDeliveryAlertsAsync();
            
            return expiryAlerts
                .Concat(overdueAlerts)
                .Concat(groupValidityAlerts)
                .Concat(groupDeliveryAlerts)
                .ToList();
        }

        public void StartPeriodicCheck(int intervalMinutes = 30)
        {
            _timer = new System.Timers.Timer(intervalMinutes * 60 * 1000);
            _timer.Elapsed += async (sender, e) => await CheckAllAlertsAsync();
            _timer.Start();
        }

        public void StopPeriodicCheck()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
