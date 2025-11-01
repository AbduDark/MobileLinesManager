using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using MobileLinesManager.ViewModels;

namespace MobileLinesManager.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _db;
        private int _totalLines;
        private int _availableLines;
        private int _assignedLines;
        private int _expiredLines;

        public int TotalLines
        {
            get => _totalLines;
            set { _totalLines = value; OnPropertyChanged(); }
        }

        public int AvailableLines
        {
            get => _availableLines;
            set { _availableLines = value; OnPropertyChanged(); }
        }

        public int AssignedLines
        {
            get => _assignedLines;
            set { _assignedLines = value; OnPropertyChanged(); }
        }

        public int ExpiredLines
        {
            get => _expiredLines;
            set { _expiredLines = value; OnPropertyChanged(); }
        }

        // ملاحظة: غيّر النوع من Operator إلى OperatorViewModel
        public ObservableCollection<OperatorViewModel> Operators { get; set; }

        public DashboardViewModel(AppDbContext db)
        {
            _db = db;
            Operators = new ObservableCollection<OperatorViewModel>();
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            var lines = await _db.Lines.Include(l => l.Group).ToListAsync();

            TotalLines = lines.Count();
            AvailableLines = lines.Count(l => !l.IsAssigned);
            AssignedLines = lines.Count(l => l.IsAssigned);
            ExpiredLines = lines.Count(l =>
                l.ExpectedReturnDate.HasValue &&
                l.ExpectedReturnDate.Value < System.DateTime.Today);

            var operators = await _db.Operators
                                     .Include(o => o.Groups)
                                     .ThenInclude(g => g.Lines)
                                     .ToListAsync();

            Operators.Clear();
            foreach (var op in operators)
            {
                var opViewModel = new OperatorViewModel
                {
                    Id = op.Id,
                    Name = op.Name,
                    ColorHex = op.ColorHex,
                    Groups = op.Groups.ToList()
                };
                
                Operators.Add(opViewModel);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
