using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.Data;
using MobileLinesManager.Services;
using MobileLinesManager.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MobileLinesManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize database
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Initialize(db);
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // DbContext
            services.AddDbContext<AppDbContext>();

            // Services
            services.AddSingleton<IAlertService, AlertService>();
            services.AddScoped<IImportService, ImportService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddScoped<IQRService, QRService>();
            services.AddSingleton<IBackupService, BackupService>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ViewModels.DashboardViewModel>();
            services.AddTransient<ViewModels.AuditTrailViewModel>();
            services.AddTransient<ViewModels.SettingsViewModel>();
            services.AddTransient<ViewModels.ReportsViewModel>();
            services.AddTransient<ViewModels.AssignViewModel>();
            services.AddTransient<ViewModels.LinesViewModel>();
            services.AddTransient<ViewModels.CategoriesViewModel>();

            // Windows
            services.AddTransient<MainWindow>();
        }
    }
}