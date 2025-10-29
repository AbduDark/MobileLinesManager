using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MobileLinesManager.Data;
using MobileLinesManager.Services;
using Microsoft.EntityFrameworkCore;

namespace MobileLinesManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Initialize database
            using (var scope = ServiceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.Migrate();
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
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

            // Windows
            services.AddTransient<MainWindow>();
        }
    }
}
