
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
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
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var items = new List<AlertItem>();
            var categories = await db.Categories
                .Include(c => c.Operator)
                .Where(c => c.HasExpiry)
                .ToListAsync();

            foreach (var cat in categories)
            {
                var rule = await db.AlertRules
                    .FirstOrDefaultAsync(r => r.CategoryId == cat.Id && r.AlertType == "Expiry" && r.Enabled)
                    ?? await db.AlertRules
                        .FirstOrDefaultAsync(r => r.CategoryId == null && r.AlertType == "Expiry" && r.Enabled);

                int daysBefore = rule?.DaysBeforeExpiry ?? cat.DefaultAlertDaysBeforeExpiry;

                var lines = await db.Lines
                    .Include(l => l.Category)
                        .ThenInclude(c => c.Operator)
                    .Include(l => l.AssignedTo)
                    .Where(l => l.CategoryId == cat.Id)
                    .ToListAsync();

                foreach (var line in lines)
                {
                    DateTime reference = line.AssignedAt ?? line.CreatedAt;
                    DateTime expiry = reference.AddDays(cat.ExpiryDays ?? 90);
                    var daysUntilExpiry = (expiry - DateTime.Today).TotalDays;

                    if (daysUntilExpiry <= daysBefore && daysUntilExpiry >= 0)
                    {
                        items.Add(new AlertItem
                        {
                            Line = line,
                            Message = $"خط {line.PhoneNumber} فترته تنتهي في {expiry:d} (بعد {(int)daysUntilExpiry} يوم)",
                            AlertType = "Expiry"
                        });
                    }
                    else if (daysUntilExpiry < 0)
                    {
                        items.Add(new AlertItem
                        {
                            Line = line,
                            Message = $"خط {line.PhoneNumber} منتهي منذ {expiry:d}",
                            AlertType = "Expired"
                        });
                    }
                }
            }

            return items;
        }

        public async Task<List<AlertItem>> CheckOverdueAssignmentsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var items = new List<AlertItem>();
            var overdueLines = await db.Lines
                .Include(l => l.Category)
                    .ThenInclude(c => c.Operator)
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

        public async Task<List<AlertItem>> CheckAllAlertsAsync()
        {
            var expiryAlerts = await CheckExpiryAlertsAsync();
            var overdueAlerts = await CheckOverdueAssignmentsAsync();
            return expiryAlerts.Concat(overdueAlerts).ToList();
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
