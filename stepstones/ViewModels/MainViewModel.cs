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

namespace stepstones.ViewModels
{
    public partial class MainViewModel : ObservableObject, IDialogPresenter
    {
        private readonly ILogger<MainViewModel> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IFolderDialogService _folderDialogService;
        private readonly IFileDialogService _fileDialogService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IFileService _fileService;
        private readonly IDatabaseService _databaseService;
        private readonly ISynchronizationService _synchronizationService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IMediaItemViewModelFactory _mediaItemViewModelFactory;
        private readonly IMessenger _messenger;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;
        private readonly IDataMigrationService _dataMigrationService;
        private readonly IFolderWatcherService _folderWatcherService;
        private readonly IMediaItemProcessorService _mediaItemProcessorService;

        public ObservableCollection<object> MediaItems { get; } = new();

        [ObservableProperty]
        private int _gridColumns = 4;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverlayVisible))]
        private object? _activeDialogViewModel;

        public bool IsOverlayVisible => ActiveDialogViewModel != null;

        private TaskCompletionSource<EditTagsResult>? _editTagsCompletionSource;

        private CancellationTokenSource? _filterCts;

        [ObservableProperty]
        private string? _filterText;

        [ObservableProperty]
        private int _pageSize = 24;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";

        public ObservableCollection<ToastViewModel> Toasts { get; } = new ObservableCollection<ToastViewModel>();

        [ObservableProperty]
        private bool _isMediaViewEmpty;

        [ObservableProperty]
        private string _emptyViewTitle;

        [ObservableProperty]
        private string _emptyViewSubtitle;

        private CancellationTokenSource? _activeTranscodingCts;

        public MainViewModel(ILogger<MainViewModel> logger,
                             ISettingsService settingsService,
                             IFolderDialogService folderDialogService,
                             IFileDialogService fileDialogService,
                             IMessageBoxService messageBoxService,
                             IFileService fileService,
                             IDatabaseService databaseService,
                             ISynchronizationService synchronizationService,
                             IThumbnailService thumbnailService,
                             IMediaItemViewModelFactory mediaItemViewModelFactory,
                             IMessenger messenger,
                             IFileTypeIdentifierService fileTypeIdentifierService,
                             IDataMigrationService dataMigrationService,
                             IFolderWatcherService folderWatcherService,
                             IMediaItemProcessorService mediaItemProcessorService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _folderDialogService = folderDialogService;
            _fileDialogService = fileDialogService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _synchronizationService = synchronizationService;
            _thumbnailService = thumbnailService;
            _mediaItemViewModelFactory = mediaItemViewModelFactory;
            _messenger = messenger;
            _fileTypeIdentifierService = fileTypeIdentifierService;
            _dataMigrationService = dataMigrationService;
            _folderWatcherService = folderWatcherService;
            _mediaItemProcessorService = mediaItemProcessorService;

            _messenger.Register<MediaItemDeletedMessage>(this, (recipient, message) =>
            {
                _ = LoadMediaItemsAsync();
                _logger.LogInformation("Removed deleted item from UI: '{FileName}'", message.Value.FileName);
            });

            _messenger.Register<ShowDialogMessage>(this, (recipient, message) =>
            {
                ActiveDialogViewModel = message.ViewModel;
            });

            _messenger.Register<ShowToastMessage>(this, async (recipient, message) =>
            {
                Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    var newToast = new ToastViewModel(message.Message, message.Type);
                    Toasts.Add(newToast);
                    await Task.Delay(3100);
                    Toasts.Remove(newToast);
                });
            });

            _messenger.Register<FileSystemChangesDetectedMessage>(this, (recipient, message) =>
            {
                _ = HandleFileSystemChangesAsync(message);
            });

            _messenger.Register<TranscodingStartedMessage>(this, (recipient, message) =>
            {
                _activeTranscodingCts = message.CancellationTokenSource;
            });

            logger.LogInformation("MainViewModel has been created.");

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            // load media folder saved path
            var savedPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                IsMediaViewEmpty = true;
                EmptyViewTitle = "No media folder selected";
                EmptyViewSubtitle = "Use the Folder button to select a media folder";

                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder path: {Path}", savedPath);
                await SynchronizeAndLoadAsync(savedPath);
                _folderWatcherService.StartWatching(savedPath);
            }
        }

        private async Task SynchronizeAndLoadAsync(string folderPath)
        {
            // load existing, already-processed items before processing orphans
            await LoadMediaItemsAsync();

            var orphans = await GetOrphanPathsAsync(folderPath);

            // callback for each repaired item (media items get their data updated one by one instead all at once)
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

            // ensure data is valid before checking orphans is correct
            _dataMigrationService.RunMigration(folderPath, onItemRepairedCallback);

            // if there aren't any orphans, finish
            if (orphans.Count == 0)
            {
                return;
            }

            AddPlaceholdersToView(orphans.Count);

            // start processing orphans
            await _synchronizationService.SynchronizeDataAsync(folderPath, (processedItem) =>
            {
                // when sync service sends back signal that it has processed an orphan...
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ReplacePlaceholderWithViewModel(processedItem);
                });
            });

            // to ensure that pagination controls are correct
            await LoadMediaItemsAsync();
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
            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogInformation("Load media items skipped: Media folder not set.");
                MediaItems.Clear();

                return;
            }

            _logger.LogInformation("Loading page {Page} for folder '{Path}'", CurrentPage, mediaFolderPath);

            var totalItems = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText);
            TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            var items = await _databaseService.GetAllItemsForFolderAsyncPaging(mediaFolderPath, CurrentPage, PageSize, FilterText);

            MediaItems.Clear();
            var newViewModels = new List<MediaItemViewModel>();
            foreach (var item in items)
            {
                var vm = _mediaItemViewModelFactory.Create(item);
                newViewModels.Add(vm);
                MediaItems.Add(vm);
            }

            if (MediaItems.Count == 0)
            {
                IsMediaViewEmpty = true;
                EmptyViewTitle = "This media folder is empty";
                EmptyViewSubtitle = "Use the Upload button to import some media files (e.g. pictures, videos, GIFs...)";
            }
            else
            {
                IsMediaViewEmpty = false;
            }

            _logger.LogInformation("Loaded {Count} media items.", newViewModels.Count);

            var thumbnailLoadTasks = newViewModels.Select(vm => vm.LoadThumbnailAsync()).ToList();
            await Task.WhenAll(thumbnailLoadTasks);
            _logger.LogInformation("Background thumbnail loading complete.");
        }

        [RelayCommand]
        private async Task SelectFolder()
        {
            _logger.LogInformation("'Select Folder' button clicked, opening folder dialog.");
            var selectedPath = _folderDialogService.ShowDialog();

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                _logger.LogWarning("Folder selection was cancelled by the user.");
            }
            else
            {
                try
                {
                    _logger.LogInformation("User selected folder '{Path}'", selectedPath);
                    _settingsService.SaveMediaFolderPath(selectedPath);
                    CurrentPage = 1;
                    await SynchronizeAndLoadAsync(selectedPath);
                    _messenger.Send(new ShowToastMessage($"Folder '{Path.GetFileName(selectedPath)}' loaded.", ToastNotificationType.Success));
                    _folderWatcherService.StartWatching(selectedPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load the selected folder.");
                    _messenger.Send(new ShowToastMessage("Failed to load folder.", ToastNotificationType.Error));
                }
            }
        }

        [RelayCommand]
        private async Task UploadFiles()
        {
            _logger.LogInformation("'Upload' button clicked.");

            // first check if we have a set media folder
            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Upload aborted: Media folder path has not been set.");
                _messageBoxService.Show("No media folder set", "No media folder path has been set, please set it first before attempting to upload file(s).");
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

            // temporarily stop watcher to prevent it from reacting to UploadFiles command copies
            _folderWatcherService.StopWatching();

            try
            {
                // place temporary placeholders for immediate feedback
                AddPlaceholdersToView(fileList.Count);

                // start processing the uploaded files
                await Task.Run(async () =>
                {
                    // first copy and rename all files into media folder 
                    var pathMappings = await _fileService.CopyFilesAsync(fileList, mediaFolderPath);

                    foreach (var sourcePath in fileList)
                    {
                        // if a file is not in the map, it means an error occurred during copy
                        if (!pathMappings.TryGetValue(sourcePath, out var newFilePath))
                        {
                            continue;
                        }

                        // if the new path is null, it was a duplicate
                        if (newFilePath == null)
                        {
                            continue;
                        }

                        try
                        {
                            // process file into media item object
                            var processedItem = await _mediaItemProcessorService.ProcessNewFileAsync(sourcePath, newFilePath);

                            if (processedItem != null)
                            {
                                // replace a placeholder with the processed file
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    ReplacePlaceholderWithViewModel(processedItem);
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to process uploaded file '{Path}'", sourcePath);
                        }
                    }
                });
            }
            finally
            {
                // restart watcher so that user's manually copied files are processed
                _folderWatcherService.StartWatching(mediaFolderPath);
            }

            _logger.LogInformation("Upload processing complete. Refreshing UI.");
            await LoadMediaItemsAsync();
        }

        [RelayCommand]
        private void CloseDialog(string? result)
        {
            if (ActiveDialogViewModel is EditTagsViewModel editTagsVM)
            {
                var wasSaved = result == "Save";

                var dialogResult = new EditTagsResult { WasSaved = wasSaved, NewTags = editTagsVM.TagsText };
                _editTagsCompletionSource?.SetResult(dialogResult);
            }
            else if (ActiveDialogViewModel is TranscodingProgressViewModel)
            {
                _activeTranscodingCts?.Cancel();
                _logger.LogInformation("User cancelled the transcoding operation via dialog's overlay.");
            }

            ActiveDialogViewModel = null;
        }

        public Task<EditTagsResult> ShowEditTagsDialogAsync(string? currentTags)
        {
            _editTagsCompletionSource = new TaskCompletionSource<EditTagsResult>();
            var dialogViewModel = new EditTagsViewModel(currentTags);
            ActiveDialogViewModel = dialogViewModel;
            return _editTagsCompletionSource.Task;
        }

        partial void OnFilterTextChanged(string? value)
        {
            CurrentPage = 1;

            _filterCts?.Cancel();
            _filterCts = new CancellationTokenSource();
            _ = TriggerFilterAsync(_filterCts.Token);
        }

        private async Task TriggerFilterAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(300, token);

                _logger.LogInformation("Filter text changed, reloading media items.");
                await LoadMediaItemsAsync();
            }
            catch (TaskCanceledException)
            {
                // expected catch, when user is typing quickly
            }
        }

        partial void OnCurrentPageChanged(int value)
        {
            OnPropertyChanged(nameof(PageInfo));
            GoToNextPageCommand.NotifyCanExecuteChanged();
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToFirstPageCommand.NotifyCanExecuteChanged();
            GoToLastPageCommand.NotifyCanExecuteChanged();
        }

        partial void OnTotalPagesChanged(int value)
        {
            OnPropertyChanged(nameof(PageInfo));
            GoToNextPageCommand.NotifyCanExecuteChanged();
            GoToPreviousPageCommand.NotifyCanExecuteChanged();
            GoToFirstPageCommand.NotifyCanExecuteChanged();
            GoToLastPageCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task GoToNextPage()
        {
            CurrentPage++;
            await LoadMediaItemsAsync();
        }

        private bool CanGoToNextPage()
        {
            return CurrentPage < TotalPages;
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task GoToPreviousPage()
        {
            CurrentPage--;
            await LoadMediaItemsAsync();
        }

        private bool CanGoToPreviousPage()
        {
            return CurrentPage > 1;
        }

        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        private async Task GoToFirstPage()
        {
            CurrentPage = 1;
            await LoadMediaItemsAsync();
        }

        private bool CanGoToFirstPage()
        {
            return CurrentPage > 1;
        }

        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        private async Task GoToLastPage()
        {
            CurrentPage = TotalPages;
            await LoadMediaItemsAsync();
        }

        private bool CanGoToLastPage()
        {
            return CurrentPage < TotalPages;
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
            if (!filePaths.Any())
            {
                return;
            }

            // add placeholders for the "to-be" processed files
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AddPlaceholdersToView(filePaths.Count);
            });

            // process files
            foreach (var filePath in filePaths)
            {
                try
                {
                    // for a file watcher, the original and final path are the same
                    var processedItem = await _mediaItemProcessorService.ProcessNewFileAsync(filePath, filePath);

                    if (processedItem != null)
                    {
                        // replace placeholder with processed media item
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ReplacePlaceholderWithViewModel(processedItem);
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process newly detected file '{Path}'", filePath);
                }
            }
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

        private void AddPlaceholdersToView(int itemCount)
        {
            var availableSlots = PageSize - MediaItems.Count; ;
            var placeholdersToAdd = Math.Min(itemCount, availableSlots);

            if (placeholdersToAdd > 0)
            {
                for (int i = 0; i < placeholdersToAdd; i++)
                {
                    MediaItems.Add(new PlaceholderViewModel());
                }
            }
        }

        private void ReplacePlaceholderWithViewModel(MediaItem newItem)
        {
            var placeholder = MediaItems.OfType<PlaceholderViewModel>().FirstOrDefault();
            if (placeholder != null)
            {
                var vm = _mediaItemViewModelFactory.Create(newItem);
                var placeholderIndex = MediaItems.IndexOf(placeholder);

                MediaItems[placeholderIndex] = vm;

                _ = vm.LoadThumbnailAsync();
            }
        }
    }
}
