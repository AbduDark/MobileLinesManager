
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MobileLinesManager.Commands;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.ViewModels
{
    public class AssignViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        
        private ObservableCollection<Line> _availableLines;
        private ObservableCollection<User> _workers;
        private ObservableCollection<AssignmentLog> _assignments;
        private Line _selectedLine;
        private User _selectedWorker;
        private AssignmentLog _selectedAssignment;
        private DateTime _expectedReturnDate = DateTime.Now.AddDays(30);
        private string _assignmentNotes;

        public AssignViewModel(AppDbContext db)
        {
            _db = db;
            
            AvailableLines = new ObservableCollection<Line>();
            Workers = new ObservableCollection<User>();
            Assignments = new ObservableCollection<AssignmentLog>();
            
            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AssignLineCommand = new AsyncRelayCommand(async _ => await AssignLineAsync(), _ => CanAssign());
            ReturnLineCommand = new AsyncRelayCommand(async _ => await ReturnLineAsync(), _ => SelectedAssignment != null);
            ViewHistoryCommand = new AsyncRelayCommand(async _ => await LoadAssignmentsAsync());
            
            LoadDataAsync().ConfigureAwait(false);
        }

        public ObservableCollection<Line> AvailableLines
        {
            get => _availableLines;
            set => SetProperty(ref _availableLines, value);
        }

        public ObservableCollection<User> Workers
        {
            get => _workers;
            set => SetProperty(ref _workers, value);
        }

        public ObservableCollection<AssignmentLog> Assignments
        {
            get => _assignments;
            set => SetProperty(ref _assignments, value);
        }

        public Line SelectedLine
        {
            get => _selectedLine;
            set => SetProperty(ref _selectedLine, value);
        }

        public User SelectedWorker
        {
            get => _selectedWorker;
            set => SetProperty(ref _selectedWorker, value);
        }

        public AssignmentLog SelectedAssignment
        {
            get => _selectedAssignment;
            set => SetProperty(ref _selectedAssignment, value);
        }

        public DateTime ExpectedReturnDate
        {
            get => _expectedReturnDate;
            set => SetProperty(ref _expectedReturnDate, value);
        }

        public string AssignmentNotes
        {
            get => _assignmentNotes;
            set => SetProperty(ref _assignmentNotes, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand AssignLineCommand { get; }
        public ICommand ReturnLineCommand { get; }
        public ICommand ViewHistoryCommand { get; }

        private async Task LoadDataAsync()
        {
            var availableLines = await _db.Lines
                .Include(l => l.Group)
                .ThenInclude(g => g.Operator)
                .Where(l => l.Status == "Available")
                .ToListAsync();

            AvailableLines.Clear();
            foreach (var line in availableLines)
            {
                AvailableLines.Add(line);
            }

            var workers = await _db.Users
                .Where(u => u.IsActive && u.Role == "Worker")
                .ToListAsync();

            Workers.Clear();
            foreach (var worker in workers)
            {
                Workers.Add(worker);
            }

            await LoadAssignmentsAsync();
        }

        private async Task LoadAssignmentsAsync()
        {
            var assignments = await _db.AssignmentLogs
                .Include(a => a.Line)
                .ThenInclude(l => l.Group)
                .Include(a => a.ToUser)
                .OrderByDescending(a => a.AssignedAt)
                .Take(100)
                .ToListAsync();

            Assignments.Clear();
            foreach (var assignment in assignments)
            {
                Assignments.Add(assignment);
            }
        }

        private bool CanAssign()
        {
            return SelectedLine != null && SelectedWorker != null;
        }

        private async Task AssignLineAsync()
        {
            if (SelectedLine != null && SelectedWorker != null)
            {
                // Update line
                SelectedLine.Status = "Assigned";
                SelectedLine.AssignedToId = SelectedWorker.Id;
                SelectedLine.AssignedAt = DateTime.Now;
                SelectedLine.ExpectedReturnDate = ExpectedReturnDate;
                SelectedLine.UpdatedAt = DateTime.Now;

                // Create assignment log
                var log = new AssignmentLog
                {
                    LineId = SelectedLine.Id,
                    ToUserId = SelectedWorker.Id,
                    AssignedAt = DateTime.Now,
                    ExpectedReturnDate = ExpectedReturnDate,
                    Status = "Pending",
                    Notes = AssignmentNotes
                };

                _db.AssignmentLogs.Add(log);

                // Create audit trail
                var audit = new AuditTrail
                {
                    Action = "AssignLine",
                    EntityType = "Line",
                    EntityId = SelectedLine.Id,
                    UserId = null, // TODO: Add current user tracking
                    Details = $"تم تسليم الخط {SelectedLine.PhoneNumber} إلى {SelectedWorker.FullName}",
                    CreatedAt = DateTime.Now
                };

                _db.AuditTrails.Add(audit);
                await _db.SaveChangesAsync();

                MessageBox.Show(
                    $"تم تسليم الخط {SelectedLine.PhoneNumber} إلى {SelectedWorker.FullName}",
                    "نجاح",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                ClearForm();
                await LoadDataAsync();
            }
        }

        private async Task ReturnLineAsync()
        {
            if (SelectedAssignment != null && SelectedAssignment.Status == "Pending")
            {
                var result = MessageBox.Show(
                    $"هل تريد استرجاع الخط؟",
                    "تأكيد الاسترجاع",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var line = await _db.Lines.FindAsync(SelectedAssignment.LineId);
                    if (line != null)
                    {
                        line.Status = "Returned";
                        line.AssignedToId = null;
                        line.AssignedAt = null;
                        line.ExpectedReturnDate = null;
                        line.UpdatedAt = DateTime.Now;
                    }

                    SelectedAssignment.Status = "Returned";
                    SelectedAssignment.ReturnedAt = DateTime.Now;

                    var audit = new AuditTrail
                    {
                        Action = "ReturnLine",
                        EntityType = "Line",
                        EntityId = SelectedAssignment.LineId,
                        UserId = null,
                        Details = $"تم استرجاع الخط {line?.PhoneNumber}",
                        CreatedAt = DateTime.Now
                    };

                    _db.AuditTrails.Add(audit);
                    await _db.SaveChangesAsync();

                    MessageBox.Show("تم استرجاع الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                    await LoadDataAsync();
                }
            }
        }

        private void ClearForm()
        {
            SelectedLine = null;
            SelectedWorker = null;
            AssignmentNotes = string.Empty;
            ExpectedReturnDate = DateTime.Now.AddDays(30);
        }
    }
}
