using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using stepstones.Models;
using stepstones.Messages;
using stepstones.Services.Interaction;
using stepstones.Services.Data;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;
using stepstones.Enums;
using static stepstones.Resources.AppConstants;

namespace stepstones.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IFolderDialogService _folderDialogService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IFileService _fileService;
        private readonly IDatabaseService _databaseService;
        private readonly ISynchronizationService _synchronizationService;
        private readonly IMediaItemViewModelFactory _mediaItemViewModelFactory;
        private readonly IMessenger _messenger;
        private readonly IDataMigrationService _dataMigrationService;
        private readonly IFolderWatcherService _folderWatcherService;
        private readonly IMediaItemProcessorService _mediaItemProcessorService;
        private readonly IDialogService _dialogService;

        public IDialogService DialogService => _dialogService;

        public ObservableCollection<object> MediaItems { get; } = new();

        private CancellationTokenSource? _filterCts;

        [ObservableProperty]
        private string? _filterText;

        public Paginator Paginator { get; }

        public ObservableCollection<ToastViewModel> Toasts { get; } = new ObservableCollection<ToastViewModel>();

        [ObservableProperty]
        private bool _isMediaViewEmpty;

        [ObservableProperty]
        private string _emptyViewTitle;

        [ObservableProperty]
        private string _emptyViewSubtitle;

        [ObservableProperty]
        private bool _isStatusIndicatorVisible;

        [ObservableProperty]
        private string _statusIndicatorText;

        public MainViewModel(ILogger<MainViewModel> logger,
                             ISettingsService settingsService,
                             IFolderDialogService folderDialogService,
                             IFileDialogService fileDialogService,
                             IMessageBoxService messageBoxService,
                             IFileService fileService,
                             IDatabaseService databaseService,
                             ISynchronizationService synchronizationService,
                             IMediaItemViewModelFactory mediaItemViewModelFactory,
                             IMessenger messenger,
                             IDataMigrationService dataMigrationService,
                             IFolderWatcherService folderWatcherService,
                             IMediaItemProcessorService mediaItemProcessorService,
                             IDialogService dialogService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _folderDialogService = folderDialogService;
            _fileDialogService = fileDialogService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _synchronizationService = synchronizationService;
            _mediaItemViewModelFactory = mediaItemViewModelFactory;
            _messenger = messenger;
            _dataMigrationService = dataMigrationService;
            _folderWatcherService = folderWatcherService;
            _mediaItemProcessorService = mediaItemProcessorService;
            _dialogService = dialogService;

            Paginator = new Paginator(async (page) => await LoadMediaItemsAsync());

            _messenger.Register<MediaItemDeletedMessage>(this, (recipient, message) =>
            {
                _ = LoadMediaItemsAsync();
                _logger.LogInformation("Removed deleted item from UI: '{FileName}'", message.Value.FileName);
            });

            _messenger.Register<ShowToastMessage>(this, async (recipient, message) =>
            {
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var newToast = new ToastViewModel(message.Message, message.Type);
                    Toasts.Add(newToast);
                    await Task.Delay(ToastNotificationDuration);
                    Toasts.Remove(newToast);
                });
            });

            _messenger.Register<FileSystemChangesDetectedMessage>(this, (recipient, message) =>
            {
                _ = HandleFileSystemChangesAsync(message);
            });

            logger.LogInformation("MainViewModel has been created.");

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // Load the last saved media folder path
            var savedPath = await _settingsService.LoadMediaFolderPathAsync();

            // case where no media folder is set in preferences
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                IsMediaViewEmpty = true;
                EmptyViewTitle = NoMediaFolderTitle;
                EmptyViewSubtitle = NoMediaFolderSubtitle;

                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            // a media folder is set in preferences
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder path: {Path}", savedPath);

                await LoadFolderAsync(savedPath);
            }
        }

        private async Task SynchronizeAndLoadAsync(string folderPath)
        {
            await LoadMediaItemsAsync();
            RunDataMigration(folderPath);
            await HandleOrphanFilesAsync(folderPath);
        }

        private void RunDataMigration(string folderPath)
        {
            Action<MediaItem> onItemRepairedCallback = (repairedItem) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var oldViewModel = MediaItems.OfType<MediaItemViewModel>()
                                                 .FirstOrDefault(vm => vm.FilePath == repairedItem.FilePath);

                    if (oldViewModel != null)
                    {
                        var index = MediaItems.IndexOf(oldViewModel);
                        var newViewModel = _mediaItemViewModelFactory.Create(repairedItem);
                        MediaItems[index] = newViewModel;
                        _ = newViewModel.LoadThumbnailAsync();
                    }
                });
            };

            _dataMigrationService.RunMigration(folderPath, onItemRepairedCallback);
        }

        private async Task HandleOrphanFilesAsync(string folderPath)
        {
            var orphans = await GetOrphanPathsAsync(folderPath);

            // if there's no orphans
            if (orphans.Count == 0)
            {
                await LoadMediaItemsAsync();
                return;
            }

            // show the status indicator
            StatusIndicatorText = $"Found {orphans.Count} new files. Processing 0 of {orphans.Count}...";
            IsStatusIndicatorVisible = true;
            var processedCount = 0;

            try
            {
                await _synchronizationService.SynchronizeDataAsync(folderPath, (processedItem) =>
                {
                    if (processedItem != null)
                    {
                        processedCount++;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusIndicatorText = $"Found {orphans.Count} new files. Processing {processedCount} of {orphans.Count}...";
                        });
                    }
                }); 
            }
            finally
            {
                // when finished, hide the indicator and refresh the view
                IsStatusIndicatorVisible = false;
                await LoadMediaItemsAsync();
            }
        }

        private async Task<List<string>> GetOrphanPathsAsync(string folderPath)
        {
            _logger.LogInformation("Checking for orphan files in '{Path}'", folderPath);

            var filesInFolder = _fileService.GetAllFiles(folderPath).ToList();
            var filePathsInDatabase = await _databaseService.GetFilePathsForFolderAsync(folderPath);
            var orphans = filesInFolder.Except(filePathsInDatabase).ToList();

            _logger.LogInformation("Found {Count} orphan files to process.", orphans.Count);
            return orphans;
        }

        private async Task LoadMediaItemsAsync()
        {
            // Get the stored media folder path
            var mediaFolderPath = await _settingsService.LoadMediaFolderPathAsync();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogInformation("Load media items skipped: Media folder not set.");

                MediaItems.Clear();
                return;
            }

            // If media folder path was successful
            _logger.LogInformation("Loading page {Page} for folder '{Path}'", Paginator.CurrentPage, mediaFolderPath);

            // Update pagination controls
            var totalItems = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText);
            Paginator.UpdateTotalPages(totalItems);

            // Load media items associated with the loaded media folder
            var items = await _databaseService.GetAllItemsForFolderAsyncPaging(mediaFolderPath, Paginator.CurrentPage, Paginator.PageSize, FilterText);

            MediaItems.Clear();
            var newViewModels = new List<MediaItemViewModel>();
            foreach (var item in items)
            {
                var vm = _mediaItemViewModelFactory.Create(item);
                newViewModels.Add(vm);
                MediaItems.Add(vm);
            }

            // If there are no media items for the associated media folder
            if (MediaItems.Count == 0)
            {
                IsMediaViewEmpty = true;
                EmptyViewTitle = EmptyFolderTitle;
                EmptyViewSubtitle = EmptyFolderSubtitle;
            }
            else
            {
                IsMediaViewEmpty = false;
            }

            _logger.LogInformation("Loaded {Count} media items.", newViewModels.Count);

            // Load thumbnails for each media item in MediaItems
            var thumbnailLoadTasks = newViewModels.Select(vm => vm.LoadThumbnailAsync()).ToList();
            _logger.LogInformation("Background thumbnail loading complete.");
        }

        [RelayCommand]
        private async Task SelectFolder()
        {
            _logger.LogInformation("'Select Folder' button clicked, opening folder dialog.");
            var selectedPath = _folderDialogService.ShowDialog();

            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                await SetNewMediaFolderAsync(selectedPath);
            }
            else
            {
                _logger.LogWarning("Folder selection was cancelled by the user.");
            }
        }

        private async Task LoadFolderAsync(string folderPath)
        {
            try
            {
                Paginator.CurrentPage = 1;
                await SynchronizeAndLoadAsync(folderPath);
                _folderWatcherService.StartWatching(folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load the selected folder.");
                _messenger.Send(new ShowToastMessage(FolderLoadErrorMessage, ToastNotificationType.Error));
            }
        }

        private async Task SetNewMediaFolderAsync(string folderPath)
        {
            _logger.LogInformation("User selected new media folder: '{Path}'", folderPath);
            _settingsService.SaveMediaFolderPath(folderPath);

            await LoadFolderAsync(folderPath);

            var toastMessage = string.Format(FolderLoadSuccessMessage, Path.GetFileName(folderPath));
            _messenger.Send(new ShowToastMessage(toastMessage, ToastNotificationType.Success));
        }

        [RelayCommand]
        private async Task UploadFiles()
        {
            _logger.LogInformation("Upload button clicked.");

            // first check if we have a set media folder
            var mediaFolderPath = await _settingsService.LoadMediaFolderPathAsync();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Upload aborted: Media folder path has not been set.");
                _messageBoxService.Show(NoMediaFolderSetTitle, NoMediaFolderSetMessage);
                return;
            }

            // retrieve file(s) user wants to upload
            var selectedFiles = _fileDialogService.ShowDialog();
            if (selectedFiles == null || !selectedFiles.Any())
            {
                _logger.LogInformation("File selection was cancelled or no files were selected.");
                return;
            }

            var fileList = selectedFiles.ToList();
            _logger.LogInformation("{FileCount} file(s) have been selected for upload.", fileList.Count);

            IsMediaViewEmpty = false;

            StatusIndicatorText = $"Processing 0 of {fileList.Count} files...";
            IsStatusIndicatorVisible = true;

            _ = Task.Run(async () =>
            {
                // temporarily stop watcher to prevent it from reacting to UploadFiles command copies
                _folderWatcherService.StopWatching();

                var processedCount = 0;
                try
                {
                    await _fileService.CopyFilesAsync(fileList, mediaFolderPath);

                    await _synchronizationService.SynchronizeDataAsync(mediaFolderPath, (processedItem) =>
                    {
                        if (processedItem != null)
                        {
                            processedCount++;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusIndicatorText = $"Processing {processedCount} of {fileList.Count} files...";
                            });
                        }
                    });
                }
                finally
                {
                    _folderWatcherService.StartWatching(mediaFolderPath);

                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        IsStatusIndicatorVisible = false;
                        await LoadMediaItemsAsync();
                        var toastMessage = string.Format(UploadSuccessMessage, processedCount, fileList.Count);
                        _messenger.Send(new ShowToastMessage(toastMessage, ToastNotificationType.Success));
                    });
                }
            });
        }

        partial void OnFilterTextChanged(string? value)
        {
            Paginator.CurrentPage = 1;

            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            _ = TriggerFilterAsync(_filterCts.Token);
        }

        private async Task TriggerFilterAsync(CancellationToken token)
        {
            try
            {
                // debounce
                await Task.Delay(FilterTriggerDelay, token);

                _logger.LogInformation("Filter text changed, reloading media items.");
                await LoadMediaItemsAsync();
            }
            catch (TaskCanceledException)
            {
                // expected catch, when user is typing quickly
            }
        }

        private async Task HandleFileSystemChangesAsync(FileSystemChangesDetectedMessage message)
        {
            await HandleRenamedFilesAsync(message.RenamedFilePaths);
            await HandleDeletedFilesAsync(message.DeletedFilePaths);
            await AddNewMediaItemsAsync(message.NewFilePaths);

            // refresh for pagination
            await Application.Current.Dispatcher.InvokeAsync(LoadMediaItemsAsync);
        }

        private async Task AddNewMediaItemsAsync(List<string> filePaths)
        {
            if (filePaths.Count == 0)
            {
                return;
            }

            _logger.LogInformation("Processing {Count} newly detected files from file watcher.", filePaths.Count);

            foreach (var filePath in filePaths)
            {
                try
                {
                    await _mediaItemProcessorService.ProcessNewFileAsync(filePath, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process newly detected file '{Path}'", filePath);
                }
            }

            await Application.Current.Dispatcher.InvokeAsync(LoadMediaItemsAsync);
        }

        private async Task HandleRenamedFilesAsync(Dictionary<string, string> renamedFiles)
        {
            if (!renamedFiles.Any())
            {
                return;
            }

            _logger.LogInformation("Processing {Count} renamed files...", renamedFiles.Count);
            foreach (var renamedFile in renamedFiles)
            {
                try
                {
                    await _databaseService.UpdateItemPathAsync(renamedFile.Key, renamedFile.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process rename from '{OldPath}' to '{NewPath}'", renamedFile.Key, renamedFile.Value);
                }
            }
        }

        private async Task HandleDeletedFilesAsync(List<string> deletedFiles)
        {
            if (!deletedFiles.Any())
            {
                return;
            }

            _logger.LogInformation("Processing {Count} deleted files...", deletedFiles.Count);
            try
            {
                await _databaseService.DeleteItemsByPathsAsync(deletedFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch deletion of files.");
            }
        }
    }
}
