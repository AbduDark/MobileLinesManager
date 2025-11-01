
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.Commands;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using MobileLinesManager.Services;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.ViewModels
{
    public class ReportsViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        private readonly IReportService _reportService;
        
        private ObservableCollection<Operator> _operators;
        private ObservableCollection<Group> _groups;
        private int? _selectedOperatorId;
        private int? _selectedGroupId;
        private DateTime _startDate = DateTime.Now.AddMonths(-1);
        private DateTime _endDate = DateTime.Now;
        private string _reportResult;
        private ObservableCollection<AlertItem> _alerts;

        public ReportsViewModel() : this(
            ServiceLocator.ServiceProvider?.GetService<AppDbContext>() ?? new AppDbContext(),
            ServiceLocator.ServiceProvider?.GetService<IReportService>() ?? new ReportService(new AppDbContext()))
        {
        }

        public ReportsViewModel(AppDbContext db, IReportService reportService)
        {
            _db = db;
            _reportService = reportService;
            
            Operators = new ObservableCollection<Operator>();
            Groups = new ObservableCollection<Group>();
            Alerts = new ObservableCollection<AlertItem>();
            
            LoadOperatorsCommand = new AsyncRelayCommand(async _ => await LoadOperatorsAsync());
            GenerateCountReportCommand = new AsyncRelayCommand(async _ => await GenerateCountReportAsync());
            GenerateExpiryReportCommand = new AsyncRelayCommand(async _ => await GenerateExpiryReportAsync());
            GenerateDelayReportCommand = new AsyncRelayCommand(async _ => await GenerateDelayReportAsync());
            GenerateAssignmentReportCommand = new AsyncRelayCommand(async _ => await GenerateAssignmentReportAsync());
            ExportToPdfCommand = new AsyncRelayCommand(async _ => await ExportToPdfAsync());
            ExportToExcelCommand = new AsyncRelayCommand(async _ => await ExportToExcelAsync());
            
            LoadOperatorsAsync().ConfigureAwait(false);
        }

        public ObservableCollection<Operator> Operators
        {
            get => _operators;
            set => SetProperty(ref _operators, value);
        }

        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        public ObservableCollection<AlertItem> Alerts
        {
            get => _alerts;
            set => SetProperty(ref _alerts, value);
        }

        public int? SelectedOperatorId
        {
            get => _selectedOperatorId;
            set
            {
                if (SetProperty(ref _selectedOperatorId, value))
                {
                    LoadGroupsByOperatorAsync(value).ConfigureAwait(false);
                }
            }
        }

        public int? SelectedGroupId
        {
            get => _selectedGroupId;
            set => SetProperty(ref _selectedGroupId, value);
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

        public string ReportResult
        {
            get => _reportResult;
            set => SetProperty(ref _reportResult, value);
        }

        public ICommand LoadOperatorsCommand { get; }
        public ICommand GenerateCountReportCommand { get; }
        public ICommand GenerateExpiryReportCommand { get; }
        public ICommand GenerateDelayReportCommand { get; }
        public ICommand GenerateAssignmentReportCommand { get; }
        public ICommand ExportToPdfCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        private async Task LoadOperatorsAsync()
        {
            var operators = await _db.Operators.ToListAsync();
            Operators.Clear();
            foreach (var op in operators)
            {
                Operators.Add(op);
            }
        }

        private async Task LoadGroupsByOperatorAsync(int? operatorId)
        {
            Groups.Clear();
            
            if (operatorId.HasValue)
            {
                var groups = await _db.Groups
                    .Where(g => g.OperatorId == operatorId.Value)
                    .ToListAsync();
                
                foreach (var grp in groups)
                {
                    Groups.Add(grp);
                }
            }
        }

        private async Task GenerateCountReportAsync()
        {
            var report = await _reportService.GenerateCountByOperatorAndGroupReportAsync();
            ReportResult = report;
            
            MessageBox.Show("تم إنشاء التقرير", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task GenerateExpiryReportAsync()
        {
            var report = await _reportService.GenerateExpiringLinesReportAsync(30);
            ReportResult = report;
            
            MessageBox.Show("تم إنشاء تقرير الخطوط المنتهية", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task GenerateDelayReportAsync()
        {
            var report = await _reportService.GenerateWorkerDelayReportAsync();
            ReportResult = report;
            
            MessageBox.Show("تم إنشاء تقرير التأخير", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task GenerateAssignmentReportAsync()
        {
            var report = await _reportService.GenerateAssignmentHistoryReportAsync(StartDate, EndDate);
            ReportResult = report;
            
            MessageBox.Show("تم إنشاء تقرير التسليمات", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ExportToPdfAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"تقرير_{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                await _reportService.ExportReportToPdfAsync(ReportResult, dialog.FileName);
                MessageBox.Show("تم تصدير التقرير بصيغة PDF", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task ExportToExcelAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"تقرير_{DateTime.Now:yyyyMMdd}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                var lines = await _db.Lines
                    .Include(l => l.Group)
                    .ThenInclude(g => g.Operator)
                    .Include(l => l.AssignedTo)
                    .ToListAsync();

                await _reportService.ExportLinesToExcelAsync(lines, dialog.FileName);
                MessageBox.Show("تم تصدير البيانات بصيغة Excel", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
