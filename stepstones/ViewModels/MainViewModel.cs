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
using System.Threading.Tasks;

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
        [NotifyPropertyChangedFor(nameof(IsOverlayVisible))]
        private object? _activeDialogViewModel;

        public bool IsOverlayVisible => ActiveDialogViewModel != null;

        private TaskCompletionSource<EditTagsResult>? _editTagsCompletionSource;

        private CancellationTokenSource? _filterCts;

        [ObservableProperty]
        private string? _filterText;

        [ObservableProperty]
        private int _pageSize = DefaultPageSize;

        [ObservableProperty]
        private int _currentPage = 1;

        [ObservableProperty]
        private int _totalPages = 1;

        public string PageInfo => string.Format(PageInfoFormat, CurrentPage, TotalPages);

        public ObservableCollection<ToastViewModel> Toasts { get; } = new ObservableCollection<ToastViewModel>();

        [ObservableProperty]
        private bool _isMediaViewEmpty;

        [ObservableProperty]
        private string _emptyViewTitle;

        [ObservableProperty]
        private string _emptyViewSubtitle;

        private CancellationTokenSource? _activeTranscodingCts;

        private bool _isProcessingUpload = false;
        private int _pendingFilesCount = 0;
        private int _processedFilesCount = 0;

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
                    await Task.Delay(ToastNotificationDuration);
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
                EmptyViewTitle = NoMediaFolderTitle;
                EmptyViewSubtitle = NoMediaFolderSubtitle;

                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder path: {Path}", savedPath);
                await LoadFolderAsync(savedPath, showToast: false);
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
                await LoadMediaItemsAsync();
                return;
            }

            _isProcessingUpload = true;
            _pendingFilesCount = orphans.Count;
            _processedFilesCount = 0;

            var currentDbItemCount = await _databaseService.GetItemCountForFolderAsync(folderPath, FilterText);
            var totalVirtualItems = currentDbItemCount + _pendingFilesCount;
            TotalPages = (int)Math.Ceiling((double)totalVirtualItems / PageSize);

            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            await LoadMediaItemsAsync();

            try
            {
                await _synchronizationService.SynchronizeDataAsync(folderPath, (processedItem) =>
                {
                    _pendingFilesCount--;

                    if (processedItem != null)
                    {
                        _processedFilesCount++;
                        // when sync service sends back signal that it has processed an orphan...
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ReplacePlaceholderWithViewModel(processedItem);
                        });
                    }
                });
            }
            finally
            {
                _isProcessingUpload = false;
                _pendingFilesCount = 0;
                _processedFilesCount = 0;
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
            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogInformation("Load media items skipped: Media folder not set.");
                MediaItems.Clear();

                return;
            }

            _logger.LogInformation("Loading page {Page} for folder '{Path}'", CurrentPage, mediaFolderPath);

            if (!_isProcessingUpload)
            {
                var totalItems = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText);
                TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);
                if (TotalPages == 0)
                {
                    TotalPages = 1;
                }
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

            if (_isProcessingUpload)
            {
                var currentDbItemCount = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText);
                var startItemIndexOfPage = (CurrentPage - 1) * PageSize;
                var placeholdersToAdd = 0;

                if (startItemIndexOfPage >= currentDbItemCount)
                {
                    var pendingItemsAlreadyShown = startItemIndexOfPage - currentDbItemCount;
                    var pendingItemsLeft = _pendingFilesCount - pendingItemsAlreadyShown;
                    placeholdersToAdd = Math.Max(0, Math.Min(PageSize, pendingItemsLeft));
                }
                else if (MediaItems.Count < PageSize)
                {
                    placeholdersToAdd = Math.Max(0, Math.Min(PageSize - MediaItems.Count, _pendingFilesCount));
                }

                for (int i = 0; i < placeholdersToAdd; i++)
                {
                    MediaItems.Add(new PlaceholderViewModel());
                }
            }

            if (MediaItems.Count == 0 && !_isProcessingUpload)
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

            var thumbnailLoadTasks = newViewModels.Select(vm => vm.LoadThumbnailAsync()).ToList();
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
                await LoadFolderAsync(selectedPath, showToast: true);
            }
        }

        private async Task LoadFolderAsync(string folderPath, bool showToast)
        {
            try
            {
                _logger.LogInformation("User selected folder '{Path}'", folderPath);
                _settingsService.SaveMediaFolderPath(folderPath);
                CurrentPage = 1;
                await SynchronizeAndLoadAsync(folderPath);

                if (showToast)
                {
                    var toastMessage = string.Format(FolderLoadSuccessMessage, Path.GetFileName(folderPath));
                    _messenger.Send(new ShowToastMessage(toastMessage, ToastNotificationType.Success));
                }

                _folderWatcherService.StartWatching(folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load the selected folder.");
                _messenger.Send(new ShowToastMessage(FolderLoadErrorMessage, ToastNotificationType.Error));
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

            _isProcessingUpload = true;
            _pendingFilesCount = fileList.Count;
            _processedFilesCount = 0;

            var currentDbItemCount = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText);
            var totalVirtualItems = currentDbItemCount + _pendingFilesCount;
            TotalPages = (int)Math.Ceiling((double)totalVirtualItems / PageSize);

            if (TotalPages == 0)
            {
                TotalPages = 1;
            }

            await LoadMediaItemsAsync();

            _ = Task.Run(async () =>
            {
                // temporarily stop watcher to prevent it from reacting to UploadFiles command copies
                _folderWatcherService.StopWatching();

                try
                {
                    await _fileService.CopyFilesAsync(fileList, mediaFolderPath);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        SynchronizeAndLoadAsync(mediaFolderPath);
                    });
                }
                finally
                {
                    _folderWatcherService.StartWatching(mediaFolderPath);
                }
            });
        }

        [RelayCommand]
        private void CloseDialog(string? result)
        {
            if (ActiveDialogViewModel is EditTagsViewModel editTagsVM)
            {
                var wasSaved = result == SaveCommandParameter;

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

        private async Task ReplacePlaceholderWithViewModel(MediaItem newItem)
        {
            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                return;
            }

            var preExistingItemCount = await _databaseService.GetItemCountForFolderAsync(mediaFolderPath, FilterText) - _processedFilesCount;
            var newItemAbsoluteIndex = preExistingItemCount + _processedFilesCount - 1;
            var pageStartIndex = (CurrentPage - 1) * PageSize;
            var pageEndIndex = pageStartIndex + PageSize - 1;

            if (newItemAbsoluteIndex >= pageStartIndex && newItemAbsoluteIndex <= pageEndIndex)
            {
                var placeholder = MediaItems.OfType<PlaceholderViewModel>().FirstOrDefault();
                if (placeholder != null)
                {
                    var vm = _mediaItemViewModelFactory.Create(newItem);
                    var placeholderIndex = MediaItems.IndexOf(placeholder);

                    if (placeholderIndex >= 0 && placeholderIndex < MediaItems.Count)
                    {
                        MediaItems[placeholderIndex] = vm;
                        _ = vm.LoadThumbnailAsync();
                    }
                }
            }
        }
    }
}
