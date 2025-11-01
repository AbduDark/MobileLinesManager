using System;
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
        private ObservableCollection<Group> _groups;
        private Line _selectedLine;
        private int? _selectedGroupId;
        private string _searchText;
        private string _phoneNumber;
        private string _serialNumber;
        private string _associatedName;
        private string _nationalId;
        private string _cashWalletId;
        private string _notes;
        private bool _isEditing;
        private Group _currentGroup;

        public LinesViewModel(AppDbContext db, IImportService importService, IQRService qrService)
        {
            _db = db;
            _importService = importService;
            _qrService = qrService;

            Lines = new ObservableCollection<Line>();
            Groups = new ObservableCollection<Group>();

            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddLineCommand = new AsyncRelayCommand(async _ => await AddLineAsync(), _ => CanAddLine());
            EditLineCommand = new RelayCommand(_ => EditLine(), _ => SelectedLine != null);
            DeleteLineCommand = new AsyncRelayCommand(async _ => await DeleteLineAsync(), _ => SelectedLine != null);
            SaveLineCommand = new AsyncRelayCommand(async _ => await SaveLineAsync(), _ => IsEditing && CanSaveLine());
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
            SearchCommand = new AsyncRelayCommand(async _ => await SearchLinesAsync());
            ImportCSVCommand = new AsyncRelayCommand(async _ => await ImportCSVAsync());
            ImportQRCommand = new AsyncRelayCommand(async _ => await ImportQRAsync());
            ScanFromWebcamCommand = new AsyncRelayCommand(async _ => await ScanFromWebcamAsync());

            LoadDataAsync().ConfigureAwait(false);
        }

        public ObservableCollection<Line> Lines
        {
            get => _lines;
            set => SetProperty(ref _lines, value);
        }

        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        public Line SelectedLine
        {
            get => _selectedLine;
            set => SetProperty(ref _selectedLine, value);
        }

        public int? SelectedGroupId
        {
            get => _selectedGroupId;
            set
            {
                if (SetProperty(ref _selectedGroupId, value))
                {
                    LoadGroupDetails(value).ConfigureAwait(false);
                }
            }
        }

        public Group CurrentGroup
        {
            get => _currentGroup;
            set => SetProperty(ref _currentGroup, value);
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

        public string AssociatedName
        {
            get => _associatedName;
            set => SetProperty(ref _associatedName, value);
        }

        public string NationalId
        {
            get => _nationalId;
            set => SetProperty(ref _nationalId, value);
        }

        public string CashWalletId
        {
            get => _cashWalletId;
            set => SetProperty(ref _cashWalletId, value);
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
        public ICommand ScanFromWebcamCommand { get; }
        public ICommand GenerateQRCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                var groups = await _db.Groups
                    .Include(g => g.Operator)
                    .Include(g => g.Lines)
                    .ToListAsync();

                Groups.Clear();
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }

                await LoadLinesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadLinesAsync()
        {
            try
            {
                var query = _db.Lines
                    .Include(l => l.Group)
                        .ThenInclude(g => g.Operator)
                    .Include(l => l.AssignedTo)
                    .AsQueryable();

                if (SelectedGroupId.HasValue)
                {
                    query = query.Where(l => l.GroupId == SelectedGroupId.Value);
                }

                var lines = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

                Lines.Clear();
                foreach (var line in lines)
                {
                    Lines.Add(line);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الخطوط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadGroupDetails(int? groupId)
        {
            if (groupId.HasValue)
            {
                CurrentGroup = await _db.Groups
                    .Include(g => g.Operator)
                    .Include(g => g.Lines)
                    .FirstOrDefaultAsync(g => g.Id == groupId.Value);

                await LoadLinesAsync();
            }
            else
            {
                CurrentGroup = null;
            }
        }

        private bool CanAddLine()
        {
            return !string.IsNullOrWhiteSpace(PhoneNumber) && SelectedGroupId.HasValue;
        }

        private bool CanSaveLine()
        {
            return !string.IsNullOrWhiteSpace(PhoneNumber) && SelectedGroupId.HasValue;
        }

        private async Task AddLineAsync()
        {
            try
            {
                if (CurrentGroup != null && CurrentGroup.IsFull)
                {
                    MessageBox.Show($"المجموعة ممتلئة (الحد الأقصى {CurrentGroup.MaxLinesCount} خط)", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var exists = await _db.Lines.AnyAsync(l => l.PhoneNumber == PhoneNumber);
                if (exists)
                {
                    MessageBox.Show($"الرقم {PhoneNumber} موجود بالفعل", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var line = new Line
                {
                    GroupId = SelectedGroupId.Value,
                    PhoneNumber = PhoneNumber,
                    SerialNumber = SerialNumber ?? string.Empty,
                    AssociatedName = AssociatedName ?? string.Empty,
                    NationalId = NationalId ?? string.Empty,
                    CashWalletId = CashWalletId ?? string.Empty,
                    Status = "Available",
                    Notes = Notes ?? string.Empty,
                    CreatedAt = DateTime.Now
                };

                _db.Lines.Add(line);
                await _db.SaveChangesAsync();

                ClearForm();
                await LoadLinesAsync();

                MessageBox.Show("تم إضافة الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة الخط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditLine()
        {
            if (SelectedLine != null)
            {
                PhoneNumber = SelectedLine.PhoneNumber;
                SerialNumber = SelectedLine.SerialNumber;
                AssociatedName = SelectedLine.AssociatedName;
                NationalId = SelectedLine.NationalId;
                CashWalletId = SelectedLine.CashWalletId;
                Notes = SelectedLine.Notes;
                SelectedGroupId = SelectedLine.GroupId;
                IsEditing = true;
            }
        }

        private async Task SaveLineAsync()
        {
            try
            {
                if (SelectedLine != null)
                {
                    SelectedLine.PhoneNumber = PhoneNumber;
                    SelectedLine.SerialNumber = SerialNumber ?? string.Empty;
                    SelectedLine.AssociatedName = AssociatedName ?? string.Empty;
                    SelectedLine.NationalId = NationalId ?? string.Empty;
                    SelectedLine.CashWalletId = CashWalletId ?? string.Empty;
                    SelectedLine.Notes = Notes ?? string.Empty;
                    SelectedLine.GroupId = SelectedGroupId.Value;
                    SelectedLine.UpdatedAt = DateTime.Now;

                    await _db.SaveChangesAsync();

                    ClearForm();
                    IsEditing = false;
                    await LoadLinesAsync();

                    MessageBox.Show("تم تحديث الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث الخط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    try
                    {
                        _db.Lines.Remove(SelectedLine);
                        await _db.SaveChangesAsync();
                        await LoadLinesAsync();

                        MessageBox.Show("تم حذف الخط بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطأ في حذف الخط: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
            AssociatedName = string.Empty;
            NationalId = string.Empty;
            CashWalletId = string.Empty;
            Notes = string.Empty;
        }

        private async Task SearchLinesAsync()
        {
            try
            {
                var query = _db.Lines
                    .Include(l => l.Group)
                        .ThenInclude(g => g.Operator)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(l =>
                        l.PhoneNumber.Contains(SearchText) ||
                        l.SerialNumber.Contains(SearchText) ||
                        l.AssociatedName.Contains(SearchText) ||
                        l.NationalId.Contains(SearchText) ||
                        l.CashWalletId.Contains(SearchText));
                }

                if (SelectedGroupId.HasValue)
                {
                    query = query.Where(l => l.GroupId == SelectedGroupId.Value);
                }

                var lines = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

                Lines.Clear();
                foreach (var line in lines)
                {
                    Lines.Add(line);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ImportCSVAsync()
        {
            if (!SelectedGroupId.HasValue)
            {
                MessageBox.Show("الرجاء اختيار مجموعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var result = await _importService.ImportFromCSVAsync(dialog.FileName, SelectedGroupId.Value);

                    if (result.SuccessfulLines.Count > 0)
                    {
                        await LoadLinesAsync();
                    }

                    var message = $"تم استيراد {result.SuccessfulLines.Count} من {result.TotalProcessed} خط";
                    if (result.Errors.Count > 0)
                    {
                        message += $"\n\nالأخطاء:\n{string.Join("\n", result.Errors)}";
                    }

                    MessageBox.Show(message, "نتيجة الاستيراد", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في استيراد الملف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ImportQRAsync()
        {
            if (!SelectedGroupId.HasValue)
            {
                MessageBox.Show("الرجاء اختيار مجموعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("ميزة مسح QR ستكون متاحة قريباً", "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task ScanQRAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp|All Files|*.*",
                    Title = "اختر صورة QR"
                };

                if (dialog.ShowDialog() == true)
                {
                    var line = await _qrService.ScanFromImageAsync(dialog.FileName);

                    if (line != null)
                    {
                        if (CurrentGroup != null)
                        {
                            line.GroupId = CurrentGroup.Id;
                        }

                        PhoneNumber = line.PhoneNumber;
                        SerialNumber = line.SerialNumber;
                        AssociatedName = line.AssociatedName;
                        NationalId = line.NationalId;
                        CashWalletId = line.CashWalletId;
                        Notes = line.Notes;

                        MessageBox.Show("تم مسح QR بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("فشل مسح QR. تأكد من أن الصورة تحتوي على رمز QR صحيح.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في مسح QR: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ScanFromWebcamAsync()
        {
            try
            {
                var scannerWindow = new Views.QRScannerWindow(_qrService);

                if (scannerWindow.ShowDialog() == true && scannerWindow.ScannedLine != null)
                {
                    var line = scannerWindow.ScannedLine;

                    if (CurrentGroup != null)
                    {
                        line.GroupId = CurrentGroup.Id;
                    }

                    PhoneNumber = line.PhoneNumber;
                    SerialNumber = line.SerialNumber;
                    AssociatedName = line.AssociatedName;
                    NationalId = line.NationalId;
                    CashWalletId = line.CashWalletId;
                    Notes = line.Notes;

                    MessageBox.Show("تم مسح QR من الكاميرا بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في مسح QR من الكاميرا: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            await Task.CompletedTask;
        }
    }
}