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
    public class GroupsViewModel : ViewModelBase
    {
        private readonly AppDbContext _db;

        private ObservableCollection<Group> _groups;
        private ObservableCollection<Operator> _operators;
        private Group _selectedGroup;
        private int? _selectedOperatorId;
        private string _groupName;
        private GroupType _selectedGroupType = GroupType.WithoutCashWallet;
        private GroupStatus _selectedGroupStatus = GroupStatus.Active;
        private int _maxLinesCount = 50;
        private int _validityDays = 60;
        private int _alertDaysBeforeExpiry = 7;
        private string _deliveredToClientName;
        private DateTime? _deliveryDate;
        private DateTime? _expectedReturnDate;
        private string _groupNotes;
        private bool _isEditing;
        private string _searchText;
        private int? _filterOperatorId;

        public GroupsViewModel(AppDbContext db)
        {
            _db = db;

            Groups = new ObservableCollection<Group>();
            Operators = new ObservableCollection<Operator>();

            LoadDataCommand = new AsyncRelayCommand(async _ => await LoadDataAsync());
            AddGroupCommand = new AsyncRelayCommand(async _ => await AddGroupAsync(), _ => CanAddGroup());
            EditGroupCommand = new RelayCommand(EditGroup, _ => SelectedGroup != null);
            DeleteGroupCommand = new AsyncRelayCommand(async _ => await DeleteGroupAsync(), _ => SelectedGroup != null);
            SaveGroupCommand = new AsyncRelayCommand(async _ => await SaveGroupAsync(), _ => IsEditing);
            CancelEditCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditing);
            RenewValidityCommand = new AsyncRelayCommand(async _ => await RenewValidityAsync(), _ => SelectedGroup != null && SelectedGroup.HasCashWallet);
            ViewGroupLinesCommand = new RelayCommand(_ => ViewGroupLines(), _ => SelectedGroup != null);
            SearchCommand = new AsyncRelayCommand(async _ => await SearchGroupsAsync());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());

            _ = LoadDataAsync();
        }

        #region Properties

        public ObservableCollection<Group> Groups
        {
            get => _groups;
            set => SetProperty(ref _groups, value);
        }

        public ObservableCollection<Operator> Operators
        {
            get => _operators;
            set => SetProperty(ref _operators, value);
        }

        public Group SelectedGroup
        {
            get => _selectedGroup;
            set => SetProperty(ref _selectedGroup, value);
        }

        public int? SelectedOperatorId
        {
            get => _selectedOperatorId;
            set => SetProperty(ref _selectedOperatorId, value);
        }

        public string GroupName
        {
            get => _groupName;
            set => SetProperty(ref _groupName, value);
        }

        public GroupType SelectedGroupType
        {
            get => _selectedGroupType;
            set => SetProperty(ref _selectedGroupType, value);
        }

        public GroupStatus SelectedGroupStatus
        {
            get => _selectedGroupStatus;
            set => SetProperty(ref _selectedGroupStatus, value);
        }

        public int MaxLinesCount
        {
            get => _maxLinesCount;
            set => SetProperty(ref _maxLinesCount, value);
        }

        public int ValidityDays
        {
            get => _validityDays;
            set => SetProperty(ref _validityDays, value);
        }

        public int AlertDaysBeforeExpiry
        {
            get => _alertDaysBeforeExpiry;
            set => SetProperty(ref _alertDaysBeforeExpiry, value);
        }

        public string DeliveredToClientName
        {
            get => _deliveredToClientName;
            set => SetProperty(ref _deliveredToClientName, value);
        }

        public DateTime? DeliveryDate
        {
            get => _deliveryDate;
            set => SetProperty(ref _deliveryDate, value);
        }

        public DateTime? ExpectedReturnDate
        {
            get => _expectedReturnDate;
            set => SetProperty(ref _expectedReturnDate, value);
        }

        public string GroupNotes
        {
            get => _groupNotes;
            set => SetProperty(ref _groupNotes, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterGroups();
                }
            }
        }

        public int? FilterOperatorId
        {
            get => _filterOperatorId;
            set
            {
                if (SetProperty(ref _filterOperatorId, value))
                {
                    FilterGroups();
                }
            }
        }

        #endregion

        #region Commands

        public ICommand LoadDataCommand { get; }
        public ICommand AddGroupCommand { get; }
        public ICommand EditGroupCommand { get; }
        public ICommand DeleteGroupCommand { get; }
        public ICommand SaveGroupCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand RenewValidityCommand { get; }
        public ICommand ViewGroupLinesCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }


        #endregion

        #region Methods

        private async Task LoadDataAsync()
        {
            try
            {
                var operators = await _db.Operators.ToListAsync();
                Operators.Clear();
                foreach (var op in operators)
                {
                    Operators.Add(op);
                }

                await FilterGroups();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task FilterGroups()
        {
            try
            {
                var query = _db.Groups
                    .Include(g => g.Operator)
                    .Include(g => g.Lines)
                    .AsQueryable();

                if (FilterOperatorId.HasValue)
                {
                    query = query.Where(g => g.OperatorId == FilterOperatorId.Value);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(g => g.Name.Contains(SearchText) || g.Notes.Contains(SearchText));
                }

                var groups = await query.OrderBy(g => g.Name).ToListAsync();

                Groups.Clear();
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المجموعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddGroup()
        {
            return !string.IsNullOrWhiteSpace(GroupName) &&
                   SelectedOperatorId.HasValue &&
                   Groups.Count == 0 || !Groups.Any(g => g.Name == GroupName && g.OperatorId == SelectedOperatorId);
        }

        private void PrepareNewGroup()
        {
            ClearForm();
            IsEditing = true;
        }

        private void EditGroup(object? parameter)
        {
            if (SelectedGroup != null)
            {
                SelectedOperatorId = SelectedGroup.OperatorId;
                GroupName = SelectedGroup.Name;
                SelectedGroupType = SelectedGroup.Type;
                SelectedGroupStatus = SelectedGroup.Status;
                MaxLinesCount = SelectedGroup.MaxLinesCount;
                ValidityDays = SelectedGroup.ValidityDays ?? 60;
                AlertDaysBeforeExpiry = SelectedGroup.AlertDaysBeforeExpiry;
                DeliveredToClientName = SelectedGroup.DeliveredToClientName ?? string.Empty;
                DeliveryDate = SelectedGroup.DeliveryDate;
                ExpectedReturnDate = SelectedGroup.ExpectedReturnDate;
                GroupNotes = SelectedGroup.Notes ?? string.Empty;
                IsEditing = true;
            }
        }

        private async Task DeleteGroupAsync()
        {
            if (SelectedGroup == null) return;

            if (SelectedGroup.Lines.Any())
            {
                MessageBox.Show("لا يمكن حذف مجموعة تحتوي على خطوط. قم بحذف الخطوط أولاً.", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"هل أنت متأكد من حذف المجموعة '{SelectedGroup.Name}'؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _db.Groups.Remove(SelectedGroup);
                    await _db.SaveChangesAsync();

                    Groups.Remove(SelectedGroup);
                    SelectedGroup = null;

                    MessageBox.Show("تم حذف المجموعة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف المجموعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool CanSaveGroup()
        {
            return !string.IsNullOrWhiteSpace(GroupName) && SelectedOperatorId.HasValue && MaxLinesCount > 0;
        }

        private async Task SaveGroupAsync()
        {
            if (!CanSaveGroup()) return;

            try
            {
                Group groupToSave;
                bool isNew = SelectedGroup == null;

                if (isNew)
                {
                    groupToSave = new Group();
                    _db.Groups.Add(groupToSave);
                }
                else
                {
                    groupToSave = SelectedGroup;
                }

                groupToSave.Name = GroupName;
                groupToSave.OperatorId = SelectedOperatorId!.Value;
                groupToSave.Type = SelectedGroupType;
                groupToSave.Status = SelectedGroupStatus;
                groupToSave.MaxLinesCount = MaxLinesCount;
                groupToSave.ValidityDays = ValidityDays;
                groupToSave.AlertDaysBeforeExpiry = AlertDaysBeforeExpiry;
                groupToSave.DeliveredToClientName = string.IsNullOrWhiteSpace(DeliveredToClientName) ? null : DeliveredToClientName;
                groupToSave.DeliveryDate = DeliveryDate;
                groupToSave.ExpectedReturnDate = ExpectedReturnDate;
                groupToSave.Notes = string.IsNullOrWhiteSpace(GroupNotes) ? null : GroupNotes;

                // إذا كانت مجموعة بمحافظ كاش، نحدد تاريخ الصلاحية
                if (SelectedGroupType == GroupType.WithCashWallet && isNew)
                {
                    groupToSave.ValidityDate = DateTime.Now.AddDays(ValidityDays);
                    groupToSave.LastRenewalDate = DateTime.Now;
                }

                if (isNew)
                {
                    groupToSave.CreatedAt = DateTime.Now;
                }
                else
                {
                    groupToSave.UpdatedAt = DateTime.Now;
                }

                await _db.SaveChangesAsync();

                IsEditing = false;
                await FilterGroups();

                SelectedGroup = Groups.FirstOrDefault(g => g.Id == groupToSave.Id);

                MessageBox.Show(isNew ? "تم إضافة المجموعة بنجاح" : "تم تحديث المجموعة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ المجموعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
        }

        private async Task RenewValidityAsync()
        {
            if (SelectedGroup == null || !SelectedGroup.HasCashWallet) return;

            var result = MessageBox.Show($"هل تريد تجديد صلاحية المجموعة '{SelectedGroup.Name}' لمدة {SelectedGroup.ValidityDays ?? 60} يوم من اليوم؟", "تجديد الصلاحية", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SelectedGroup.ValidityDate = DateTime.Now.AddDays(SelectedGroup.ValidityDays ?? 60);
                    SelectedGroup.LastRenewalDate = DateTime.Now;
                    SelectedGroup.UpdatedAt = DateTime.Now;

                    await _db.SaveChangesAsync();

                    await FilterGroups();

                    MessageBox.Show($"تم تجديد الصلاحية بنجاح. التاريخ الجديد: {SelectedGroup.ValidityDate:yyyy-MM-dd}", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تجديد الصلاحية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewGroupLines()
        {
            if (SelectedGroup == null) return;

            // سيتم ربط هذا بالتنقل إلى شاشة الخطوط
            MessageBox.Show($"سيتم فتح شاشة الخطوط للمجموعة: {SelectedGroup.Name}", "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async Task AddGroupAsync()
        {
            // This method is a placeholder for future implementation if needed.
            // The logic for adding a group is handled by PrepareNewGroup and SaveGroupAsync.
            await Task.CompletedTask;
        }

        private async Task SearchGroupsAsync()
        {
            await FilterGroups();
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
            FilterOperatorId = null;
            _ = SearchGroupsAsync();
        }

        private bool CanDeleteGroup()
        {
            if (SelectedGroup == null) return false;

            var hasLines = _db.Lines.Count(l => l.GroupId == SelectedGroup.Id) > 0;
            return !hasLines;
        }

        private void ClearForm()
        {
            GroupName = string.Empty;
            SelectedOperatorId = null;
            SelectedGroupType = GroupType.WithoutCashWallet;
            SelectedGroupStatus = GroupStatus.Active;
            MaxLinesCount = 50;
            ValidityDays = 60;
            AlertDaysBeforeExpiry = 7;
            DeliveredToClientName = string.Empty;
            DeliveryDate = null;
            ExpectedReturnDate = null;
            GroupNotes = string.Empty;
            SelectedGroup = null;
        }

        #endregion
    }
}