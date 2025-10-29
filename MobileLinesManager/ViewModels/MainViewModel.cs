
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
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
        private int _totalLines;
        private int _assignedLines;
        private int _availableLines;
        private int _alertCount;
        private ObservableCollection<Operator> _operators;

        public MainViewModel(AppDbContext db, IAlertService alertService)
        {
            _db = db;
            _alertService = alertService;
            
            Operators = new ObservableCollection<Operator>();
            
            NavigateCommand = new RelayCommand(Navigate);
            RefreshDashboardCommand = new AsyncRelayCommand(async _ => await LoadDashboardDataAsync());
            
            LoadDashboardDataAsync().ConfigureAwait(false);
            _alertService.StartPeriodicCheck(30);
        }

        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
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

        public ObservableCollection<Operator> Operators
        {
            get => _operators;
            set => SetProperty(ref _operators, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand RefreshDashboardCommand { get; }

        private void Navigate(object parameter)
        {
            if (parameter is string view)
            {
                CurrentView = view;
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            TotalLines = await _db.Lines.CountAsync();
            AssignedLines = await _db.Lines.CountAsync(l => l.Status == "Assigned");
            AvailableLines = await _db.Lines.CountAsync(l => l.Status == "Available");
            
            var alerts = await _alertService.CheckAllAlertsAsync();
            AlertCount = alerts.Count;

            var operators = await _db.Operators.Include(o => o.Categories).ToListAsync();
            Operators.Clear();
            foreach (var op in operators)
            {
                Operators.Add(op);
            }
        }
    }
}
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MobileLinesManager.Commands;

namespace MobileLinesManager.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _currentView = "Dashboard";
        private int _alertCount = 0;

        public string CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public int AlertCount
        {
            get => _alertCount;
            set
            {
                _alertCount = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
        }

        private void Navigate(string view)
        {
            CurrentView = view;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
