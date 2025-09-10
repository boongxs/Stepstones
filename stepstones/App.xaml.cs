using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;
using stepstones.ViewModels;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using stepstones.Services.Interaction;
using stepstones.Services.Data;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;
using Vlc.DotNet.Wpf;

namespace stepstones
{
    public partial class App : Application
    {
        private readonly IHost _host;
        private static VlcControl? PreloadVlcPlayer;

        public App()
        {
            InitializeComponent();

            var currentAssembly = System.Reflection.Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            if (currentDirectory != null)
            {
                var vlcLibDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

                PreloadVlcPlayer = new VlcControl();
                PreloadVlcPlayer.SourceProvider.CreatePlayer(vlcLibDirectory, "--no-video");
                PreloadVlcPlayer.Dispose();
            }

                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logPath = Path.Combine(appDataPath, "stepstones", "logs", "stepstones-.txt");

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Debug()
                .WriteTo.File(logPath, 
                              rollingInterval: RollingInterval.Day,
                              outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}"
                )
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
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddTransient<ISynchronizationService, SynchronizationService>();
                    services.AddSingleton<IThumbnailService, ThumbnailService>();
                    services.AddTransient<IClipboardService, ClipboardService>();
                    services.AddTransient<IFileTypeIdentifierService, FileTypeIdentifierService>();
                    services.AddTransient<IImageDimensionService, ImageDimensionService>();
                    services.AddSingleton<IDataMigrationService, DataMigrationService>();

                    services.AddSingleton<IMediaItemViewModelFactory, MediaItemViewModelFactory>();

                    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<MainWindow>();

                    services.AddSingleton<IDialogPresenter>(s => s.GetRequiredService<MainViewModel>());
                    services.AddTransient<Lazy<IDialogPresenter>>(s => new Lazy<IDialogPresenter>(() => s.GetRequiredService<IDialogPresenter>()));
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
