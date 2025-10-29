
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MobileLinesManager.Commands;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.ViewModels
{
    public class AuditTrailViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        
        private ObservableCollection<AuditTrail> _auditTrails;
        private AuditTrail _selectedAudit;
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private string _searchEntity;
        private string _searchAction;

        public AuditTrailViewModel() : this(new AppDbContext())
        {
        }

        public AuditTrailViewModel(AppDbContext db)
        {
            _db = db;
            
            AuditTrails = new ObservableCollection<AuditTrail>();
            
            LoadAuditTrailsCommand = new AsyncRelayCommand(async _ => await LoadAuditTrailsAsync());
            FilterAuditTrailsCommand = new AsyncRelayCommand(async _ => await FilterAuditTrailsAsync());
            ClearFiltersCommand = new AsyncRelayCommand(async _ => await ClearFiltersAsync());
            
            LoadAuditTrailsAsync().ConfigureAwait(false);
        }

        public ObservableCollection<AuditTrail> AuditTrails
        {
            get => _auditTrails;
            set => SetProperty(ref _auditTrails, value);
        }

        public AuditTrail SelectedAudit
        {
            get => _selectedAudit;
            set => SetProperty(ref _selectedAudit, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public string SearchEntity
        {
            get => _searchEntity;
            set => SetProperty(ref _searchEntity, value);
        }

        public string SearchAction
        {
            get => _searchAction;
            set => SetProperty(ref _searchAction, value);
        }

        public ICommand LoadAuditTrailsCommand { get; }
        public ICommand FilterAuditTrailsCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        private async Task LoadAuditTrailsAsync()
        {
            var audits = await _db.AuditTrails
                .Include(a => a.User)
                .OrderByDescending(a => a.Timestamp)
                .Take(500)
                .ToListAsync();

            AuditTrails.Clear();
            foreach (var audit in audits)
            {
                AuditTrails.Add(audit);
            }
        }

        private async Task FilterAuditTrailsAsync()
        {
            var query = _db.AuditTrails
                .Include(a => a.User)
                .Where(a => a.Timestamp >= StartDate && a.Timestamp <= EndDate);

            if (!string.IsNullOrWhiteSpace(SearchEntity))
            {
                query = query.Where(a => a.EntityName.Contains(SearchEntity));
            }

            if (!string.IsNullOrWhiteSpace(SearchAction))
            {
                query = query.Where(a => a.Action.Contains(SearchAction));
            }

            var audits = await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            AuditTrails.Clear();
            foreach (var audit in audits)
            {
                AuditTrails.Add(audit);
            }
        }

        private async Task ClearFiltersAsync()
        {
            StartDate = DateTime.Now.AddMonths(-1);
            EndDate = DateTime.Now;
            SearchEntity = string.Empty;
            SearchAction = string.Empty;
            await LoadAuditTrailsAsync();
        }
    }
}
