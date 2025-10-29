
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MobileLinesManager.Commands;
using MobileLinesManager.Services;
using Microsoft.Win32;

namespace MobileLinesManager.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly IBackupService _backupService;
        
        private List<string> _availableBackups;
        private string _selectedBackup;
        private string _lastBackupPath;
        private bool _isProcessing;

        public SettingsViewModel(IBackupService backupService)
        {
            _backupService = backupService;
            
            CreateBackupCommand = new AsyncRelayCommand(async _ => await CreateBackupAsync());
            RestoreBackupCommand = new AsyncRelayCommand(async _ => await RestoreBackupAsync(), _ => !string.IsNullOrEmpty(SelectedBackup));
            BrowseBackupCommand = new RelayCommand(BrowseBackup);
            RefreshBackupsCommand = new RelayCommand(_ => LoadAvailableBackups());
            
            LoadAvailableBackups();
        }

        public List<string> AvailableBackups
        {
            get => _availableBackups;
            set => SetProperty(ref _availableBackups, value);
        }

        public string SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        public string LastBackupPath
        {
            get => _lastBackupPath;
            set => SetProperty(ref _lastBackupPath, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public ICommand CreateBackupCommand { get; }
        public ICommand RestoreBackupCommand { get; }
        public ICommand BrowseBackupCommand { get; }
        public ICommand RefreshBackupsCommand { get; }

        private void LoadAvailableBackups()
        {
            AvailableBackups = _backupService.GetAvailableBackups();
        }

        private async System.Threading.Tasks.Task CreateBackupAsync()
        {
            try
            {
                IsProcessing = true;
                var backupPath = await _backupService.BackupDatabaseAsync();
                LastBackupPath = backupPath;
                LoadAvailableBackups();
                MessageBox.Show($"تم إنشاء النسخة الاحتياطية بنجاح:\n{backupPath}", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل إنشاء النسخة الاحتياطية:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private async System.Threading.Tasks.Task RestoreBackupAsync()
        {
            if (string.IsNullOrEmpty(SelectedBackup))
            {
                MessageBox.Show("الرجاء اختيار نسخة احتياطية للاستعادة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "تحذير: ستؤدي هذه العملية إلى استبدال البيانات الحالية بالنسخة الاحتياطية المحددة.\nهل تريد المتابعة؟",
                "تأكيد الاستعادة",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                IsProcessing = true;
                await _backupService.RestoreDatabaseAsync(SelectedBackup);
                MessageBox.Show("تم استعادة النسخة الاحتياطية بنجاح.\nسيتم إعادة تشغيل التطبيق.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Restart application
                System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل استعادة النسخة الاحتياطية:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void BrowseBackup(object parameter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Database Files (*.db)|*.db|All Files (*.*)|*.*",
                Title = "اختر ملف النسخة الاحتياطية"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedBackup = dialog.FileName;
            }
        }
    }
}
