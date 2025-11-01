using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.Commands;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using MobileLinesManager.Services;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        private readonly IAlertService _alertService;
        private string _currentView = "Dashboard";
        private int _totalGroups;
        private int _totalLines;
        private int _assignedLines;
        private int _availableLines;
        private int _alertCount;
        private int? _selectedOperatorId;
        private ObservableCollection<Operator> _operators;

        public MainViewModel() : this(
            ServiceLocator.ServiceProvider?.GetService<AppDbContext>() ?? new AppDbContext(),
            ServiceLocator.ServiceProvider?.GetService<IAlertService>() ?? new AlertService(ServiceLocator.ServiceProvider))
        {
        }

        public MainViewModel(AppDbContext db, IAlertService alertService)
        {
            _db = db;
            _alertService = alertService;
            
            Operators = new ObservableCollection<Operator>();
            
            NavigateCommand = new RelayCommand(Navigate);
            RefreshDashboardCommand = new AsyncRelayCommand(async _ => await LoadDashboardDataAsync());
            NavigateToOperatorGroupsCommand = new RelayCommand(NavigateToOperatorGroups);
            
            LoadDashboardDataAsync().ConfigureAwait(false);
            _alertService.StartPeriodicCheck(30);
        }

        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public int TotalGroups
        {
            get => _totalGroups;
            set => SetProperty(ref _totalGroups, value);
        }

        public int TotalLines
        {
            get => _totalLines;
            set => SetProperty(ref _totalLines, value);
        }

        public int AssignedLines
        {
            get => _assignedLines;
            set => SetProperty(ref _assignedLines, value);
        }

        public int AvailableLines
        {
            get => _availableLines;
            set => SetProperty(ref _availableLines, value);
        }

        public int AlertCount
        {
            get => _alertCount;
            set => SetProperty(ref _alertCount, value);
        }

        public int? SelectedOperatorId
        {
            get => _selectedOperatorId;
            set => SetProperty(ref _selectedOperatorId, value);
        }

        public ObservableCollection<Operator> Operators
        {
            get => _operators;
            set => SetProperty(ref _operators, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand RefreshDashboardCommand { get; }
        public ICommand NavigateToOperatorGroupsCommand { get; }

        private void Navigate(object parameter)
        {
            if (parameter is string view)
            {
                CurrentView = view;
                SelectedOperatorId = null;
            }
        }

        private void NavigateToOperatorGroups(object parameter)
        {
            if (parameter is int operatorId)
            {
                SelectedOperatorId = operatorId;
                CurrentView = "Groups";
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            TotalGroups = await _db.Groups.CountAsync();
            TotalLines = await _db.Lines.CountAsync();
            AssignedLines = await _db.Lines.CountAsync(l => l.Status == "Assigned");
            AvailableLines = await _db.Lines.CountAsync(l => l.Status == "Available");
            
            var alerts = await _alertService.CheckAllAlertsAsync();
            AlertCount = alerts.Count;

            var operators = await _db.Operators.ToListAsync();
            Operators.Clear();
            foreach (var op in operators)
            {
                Operators.Add(op);
            }
        }
    }
}
