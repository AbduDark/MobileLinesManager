
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MobileLinesManager.Commands;
using MobileLinesManager.Data;
using MobileLinesManager.Models;
using MobileLinesManager.Services;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager.ViewModels
{
    public class LinesViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        private readonly IImportService _importService;
        private readonly IQRService _qrService;
        
        private ObservableCollection<Line> _lines;
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Operator> _operators;
        private Line _selectedLine;
        private int? _selectedOperatorId;
        private int? _selectedCategoryId;
        private string _searchText;
        private string _phoneNumber;
        private string _serialNumber;
        private string _walletId;
        private string _notes;
        private bool _isEditing;

        public LinesViewModel(AppDbContext db, IImportService importService, IQRService qrService)
        {
            _db = db;
            _importService = importService;
            _qrService = qrService;
            
            Lines = new ObservableCollection<Line>();
            Categories = new ObservableCollection<Category>();
            Operators = new ObservableCollection<Operator>();
            
            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddLineCommand = new AsyncRelayCommand(async _ => await AddLineAsync(), _ => CanAddLine());
            EditLineCommand = new RelayCommand(EditLine, _ => SelectedLine != null);
            DeleteLineCommand = new AsyncRelayCommand(async _ => await DeleteLineAsync(), _ => SelectedLine != null);
            SaveLineCommand = new AsyncRelayCommand(async _ => await SaveLineAsync(), _ => IsEditing);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
            SearchCommand = new AsyncRelayCommand(async _ => await SearchLinesAsync());
            ImportCSVCommand = new AsyncRelayCommand(async _ => await ImportCSVAsync());
            ImportQRCommand = new AsyncRelayCommand(async _ => await ImportQRAsync());
            
            LoadDataAsync().ConfigureAwait(false);
        }

        public ObservableCollection<Line> Lines
        {
            get => _lines;
            set => SetProperty(ref _lines, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public ObservableCollection<Operator> Operators
        {
            get => _operators;
            set => SetProperty(ref _operators, value);
        }

        public Line SelectedLine
        {
            get => _selectedLine;
            set => SetProperty(ref _selectedLine, value);
        }

        public int? SelectedOperatorId
        {
            get => _selectedOperatorId;
            set
            {
                if (SetProperty(ref _selectedOperatorId, value))
                {
                    LoadCategoriesByOperatorAsync(value).ConfigureAwait(false);
                }
            }
        }

        public int? SelectedCategoryId
        {
            get => _selectedCategoryId;
            set => SetProperty(ref _selectedCategoryId, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string SerialNumber
        {
            get => _serialNumber;
            set => SetProperty(ref _serialNumber, value);
        }

        public string WalletId
        {
            get => _walletId;
            set => SetProperty(ref _walletId, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand AddLineCommand { get; }
        public ICommand EditLineCommand { get; }
        public ICommand DeleteLineCommand { get; }
        public ICommand SaveLineCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ImportCSVCommand { get; }
        public ICommand ImportQRCommand { get; }

        private async Task LoadDataAsync()
        {
            var operators = await _db.Operators.ToListAsync();
            Operators.Clear();
            foreach (var op in operators)
            {
                Operators.Add(op);
            }

            await LoadLinesAsync();
        }

        private async Task LoadLinesAsync()
        {
            var lines = await _db.Lines
                .Include(l => l.Category)
                .ThenInclude(c => c.Operator)
                .Include(l => l.AssignedTo)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            Lines.Clear();
            foreach (var line in lines)
            {
                Lines.Add(line);
            }
        }

        private async Task LoadCategoriesByOperatorAsync(int? operatorId)
        {
            Categories.Clear();
            
            if (operatorId.HasValue)
            {
                var categories = await _db.Categories
                    .Where(c => c.OperatorId == operatorId.Value)
                    .ToListAsync();
                
                foreach (var cat in categories)
                {
                    Categories.Add(cat);
                }
            }
        }

        private bool CanAddLine()
        {
            return !string.IsNullOrWhiteSpace(PhoneNumber) && SelectedCategoryId.HasValue;
        }

        private async Task AddLineAsync()
        {
            var category = await _db.Categories.FindAsync(SelectedCategoryId.Value);
            
            var line = new Line
            {
                CategoryId = SelectedCategoryId.Value,
                PhoneNumber = PhoneNumber,
                SerialNumber = SerialNumber,
                Status = "Available",
                HasWallet = category.RequiresWallet,
                WalletId = WalletId,
                Notes = Notes
            };

            _db.Lines.Add(line);
            await _db.SaveChangesAsync();

            ClearForm();
            await LoadLinesAsync();
            
            MessageBox.Show("تم إضافة الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditLine(object parameter)
        {
            if (SelectedLine != null)
            {
                PhoneNumber = SelectedLine.PhoneNumber;
                SerialNumber = SelectedLine.SerialNumber;
                WalletId = SelectedLine.WalletId;
                Notes = SelectedLine.Notes;
                SelectedCategoryId = SelectedLine.CategoryId;
                IsEditing = true;
            }
        }

        private async Task SaveLineAsync()
        {
            if (SelectedLine != null)
            {
                SelectedLine.PhoneNumber = PhoneNumber;
                SelectedLine.SerialNumber = SerialNumber;
                SelectedLine.WalletId = WalletId;
                SelectedLine.Notes = Notes;
                SelectedLine.CategoryId = SelectedCategoryId.Value;
                SelectedLine.UpdatedAt = System.DateTime.Now;

                await _db.SaveChangesAsync();
                
                ClearForm();
                IsEditing = false;
                await LoadLinesAsync();
                
                MessageBox.Show("تم تحديث الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task DeleteLineAsync()
        {
            if (SelectedLine != null)
            {
                var result = MessageBox.Show(
                    $"هل تريد حذف الخط {SelectedLine.PhoneNumber}؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _db.Lines.Remove(SelectedLine);
                    await _db.SaveChangesAsync();
                    await LoadLinesAsync();
                    
                    MessageBox.Show("تم حذف الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void CancelEdit()
        {
            ClearForm();
            IsEditing = false;
        }

        private void ClearForm()
        {
            PhoneNumber = string.Empty;
            SerialNumber = string.Empty;
            WalletId = string.Empty;
            Notes = string.Empty;
            SelectedCategoryId = null;
            SelectedLine = null;
        }

        private async Task SearchLinesAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadLinesAsync();
                return;
            }

            var lines = await _db.Lines
                .Include(l => l.Category)
                .ThenInclude(c => c.Operator)
                .Include(l => l.AssignedTo)
                .Where(l => l.PhoneNumber.Contains(SearchText) || 
                           l.SerialNumber.Contains(SearchText) ||
                           l.WalletId.Contains(SearchText))
                .ToListAsync();

            Lines.Clear();
            foreach (var line in lines)
            {
                Lines.Add(line);
            }
        }

        private async Task ImportCSVAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "اختر ملف CSV"
            };

            if (dialog.ShowDialog() == true && SelectedCategoryId.HasValue)
            {
                var result = await _importService.ImportFromCSVAsync(dialog.FileName, SelectedCategoryId.Value);
                
                MessageBox.Show(
                    $"تم معالجة {result.TotalProcessed} سطر\nنجح: {result.SuccessCount}\nفشل: {result.Errors.Count}",
                    "نتيجة الاستيراد",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                await LoadLinesAsync();
            }
        }

        private async Task ImportQRAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "اختر صورة QR"
            };

            if (dialog.ShowDialog() == true && SelectedCategoryId.HasValue)
            {
                var qrData = await _qrService.ScanQRFromFileAsync(dialog.FileName);
                
                if (!string.IsNullOrWhiteSpace(qrData))
                {
                    var result = await _importService.ImportFromQRDataAsync(qrData, SelectedCategoryId.Value);
                    
                    MessageBox.Show(
                        $"تم استيراد {result.SuccessCount} خط من QR",
                        "نجاح",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    await LoadLinesAsync();
                }
                else
                {
                    MessageBox.Show("فشل قراءة QR", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
