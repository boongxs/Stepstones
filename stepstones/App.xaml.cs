using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using stepstones.Services;
using stepstones.ViewModels;

namespace stepstones
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logPath = Path.Combine(appDataPath, "stepstones", "logs", "stepstones-.txt");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ISettingsService, SettingsService>();
                    services.AddTransient<IFolderDialogService, FolderDialogService>();
                    services.AddTransient<IFileDialogService, FileDialogService>();
                    services.AddTransient<IMessageBoxService, MessageBoxService>();
                    services.AddTransient<IFileService, FileService>();

                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MainWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();
            Log.CloseAndFlush();

            base.OnExit(e);
        }
    }

}
