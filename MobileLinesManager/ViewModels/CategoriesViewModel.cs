
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
    public class CategoriesViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;
        
        private ObservableCollection<Category> _categories;
        private ObservableCollection<Operator> _operators;
        private Category _selectedCategory;
        private int? _selectedOperatorId;
        private string _categoryName;
        private bool _requiresWallet;
        private bool _requiresConfirmation;
        private bool _hasExpiry;
        private int _expiryDays = 90;
        private int _defaultAlertDays = 30;
        private bool _allowAddNumbers = true;
        private string _categoryNotes;
        private bool _isEditing;

        public CategoriesViewModel(AppDbContext db)
        {
            _db = db;
            
            Categories = new ObservableCollection<Category>();
            Operators = new ObservableCollection<Operator>();
            
            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddCategoryCommand = new AsyncRelayCommand(async _ => await AddCategoryAsync(), _ => CanAddCategory());
            EditCategoryCommand = new RelayCommand(EditCategory, _ => SelectedCategory != null);
            DeleteCategoryCommand = new AsyncRelayCommand(async _ => await DeleteCategoryAsync(), _ => SelectedCategory != null);
            SaveCategoryCommand = new AsyncRelayCommand(async _ => await SaveCategoryAsync(), _ => IsEditing);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
            
            LoadDataAsync().ConfigureAwait(false);
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

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public int? SelectedOperatorId
        {
            get => _selectedOperatorId;
            set => SetProperty(ref _selectedOperatorId, value);
        }

        public string CategoryName
        {
            get => _categoryName;
            set => SetProperty(ref _categoryName, value);
        }

        public bool RequiresWallet
        {
            get => _requiresWallet;
            set => SetProperty(ref _requiresWallet, value);
        }

        public bool RequiresConfirmation
        {
            get => _requiresConfirmation;
            set => SetProperty(ref _requiresConfirmation, value);
        }

        public bool HasExpiry
        {
            get => _hasExpiry;
            set => SetProperty(ref _hasExpiry, value);
        }

        public int ExpiryDays
        {
            get => _expiryDays;
            set => SetProperty(ref _expiryDays, value);
        }

        public int DefaultAlertDays
        {
            get => _defaultAlertDays;
            set => SetProperty(ref _defaultAlertDays, value);
        }

        public bool AllowAddNumbers
        {
            get => _allowAddNumbers;
            set => SetProperty(ref _allowAddNumbers, value);
        }

        public string CategoryNotes
        {
            get => _categoryNotes;
            set => SetProperty(ref _categoryNotes, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand AddCategoryCommand { get; }
        public ICommand EditCategoryCommand { get; }
        public ICommand DeleteCategoryCommand { get; }
        public ICommand SaveCategoryCommand { get; }
        public ICommand CancelEditCommand { get; }

        private async Task LoadDataAsync()
        {
            var operators = await _db.Operators.ToListAsync();
            Operators.Clear();
            foreach (var op in operators)
            {
                Operators.Add(op);
            }

            var categories = await _db.Categories
                .Include(c => c.Operator)
                .OrderBy(c => c.OperatorId)
                .ThenBy(c => c.Name)
                .ToListAsync();

            Categories.Clear();
            foreach (var cat in categories)
            {
                Categories.Add(cat);
            }
        }

        private bool CanAddCategory()
        {
            return !string.IsNullOrWhiteSpace(CategoryName) && SelectedOperatorId.HasValue;
        }

        private async Task AddCategoryAsync()
        {
            var category = new Category
            {
                OperatorId = SelectedOperatorId.Value,
                Name = CategoryName,
                RequiresWallet = RequiresWallet,
                RequiresConfirmation = RequiresConfirmation,
                HasExpiry = HasExpiry,
                ExpiryDays = ExpiryDays,
                DefaultAlertDaysBeforeExpiry = DefaultAlertDays,
                AllowAddNumbers = AllowAddNumbers,
                Notes = CategoryNotes
            };

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            ClearForm();
            await LoadDataAsync();
            
            MessageBox.Show("تم إضافة الفئة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditCategory(object parameter)
        {
            if (SelectedCategory != null)
            {
                SelectedOperatorId = SelectedCategory.OperatorId;
                CategoryName = SelectedCategory.Name;
                RequiresWallet = SelectedCategory.RequiresWallet;
                RequiresConfirmation = SelectedCategory.RequiresConfirmation;
                HasExpiry = SelectedCategory.HasExpiry;
                ExpiryDays = SelectedCategory.ExpiryDays ?? 90;
                DefaultAlertDays = SelectedCategory.DefaultAlertDaysBeforeExpiry;
                AllowAddNumbers = SelectedCategory.AllowAddNumbers;
                CategoryNotes = SelectedCategory.Notes;
                IsEditing = true;
            }
        }

        private async Task SaveCategoryAsync()
        {
            if (SelectedCategory != null)
            {
                SelectedCategory.OperatorId = SelectedOperatorId.Value;
                SelectedCategory.Name = CategoryName;
                SelectedCategory.RequiresWallet = RequiresWallet;
                SelectedCategory.RequiresConfirmation = RequiresConfirmation;
                SelectedCategory.HasExpiry = HasExpiry;
                SelectedCategory.ExpiryDays = ExpiryDays;
                SelectedCategory.DefaultAlertDaysBeforeExpiry = DefaultAlertDays;
                SelectedCategory.AllowAddNumbers = AllowAddNumbers;
                SelectedCategory.Notes = CategoryNotes;
                SelectedCategory.UpdatedAt = System.DateTime.Now;

                await _db.SaveChangesAsync();
                
                ClearForm();
                IsEditing = false;
                await LoadDataAsync();
                
                MessageBox.Show("تم تحديث الفئة بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory != null)
            {
                // تم تعطيل هذه الوظيفة - النظام الآن يستخدم Groups بدلاً من Categories
                MessageBox.Show(
                    "هذه الوظيفة لم تعد مستخدمة. الرجاء استخدام إدارة المجموعات.",
                    "تنبيه",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
        }

        private void CancelEdit()
        {
            ClearForm();
            IsEditing = false;
        }

        private void ClearForm()
        {
            CategoryName = string.Empty;
            SelectedOperatorId = null;
            RequiresWallet = false;
            RequiresConfirmation = false;
            HasExpiry = false;
            ExpiryDays = 90;
            DefaultAlertDays = 30;
            AllowAddNumbers = true;
            CategoryNotes = string.Empty;
            SelectedCategory = null;
        }
    }
}
