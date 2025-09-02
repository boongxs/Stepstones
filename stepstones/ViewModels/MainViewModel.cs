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

        public ObservableCollection<object> MediaItems { get; } = new();

        [ObservableProperty]
        private double _thumbnailWidth = 270;

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
        private int _totalPages;

        public string PageInfo => $"Page {CurrentPage} of {TotalPages}";

        public ObservableCollection<ToastViewModel> Toasts { get; } = new ObservableCollection<ToastViewModel>();

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
                             IFileTypeIdentifierService fileTypeIdentifierService)
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
                var newToast = new ToastViewModel(message.Message, message.Type);
                Toasts.Add(newToast);
                Task.Delay(3100).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(() => Toasts.Remove(newToast));
                });
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
                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder path: {Path}", savedPath);
                await SynchronizeAndLoadAsync(savedPath);
            }
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

            var items = await _databaseService.GetAllItemsForFolderAsync(mediaFolderPath, CurrentPage, PageSize, FilterText);

            MediaItems.Clear();
            var newViewModels = new List<MediaItemViewModel>();
            foreach (var item in items)
            {
                var vm = _mediaItemViewModelFactory.Create(item);
                newViewModels.Add(vm);
                MediaItems.Add(vm);
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load the selected folder.");
                    _messenger.Send(new ShowToastMessage("Failed to load folder.", ToastNotificationType.Error));
                }
            }
        }

        private async Task SynchronizeAndLoadAsync(string folderPath)
        {
            var orphans = await GetOrphanPathsAsync(folderPath);

            if (!orphans.Any())
            {
                await LoadMediaItemsAsync();
                return;
            }

            // load existing, already-processed items
            await LoadMediaItemsAsync();

            // calculate empty slots on page for placeholders
            var availableSlots = PageSize - MediaItems.Count;
            var placeholdersToAdd = Math.Min(orphans.Count, availableSlots);

            if (placeholdersToAdd > 0)
            {
                for (int i = 0; i < placeholdersToAdd; i++)
                {
                    MediaItems.Add(new PlaceholderViewModel());
                }
            }

            // start processing orphans
            await _synchronizationService.SynchronizeDataAsync(folderPath, (processedItem) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = _mediaItemViewModelFactory.Create(processedItem);
                    var placeholder = MediaItems.OfType<PlaceholderViewModel>().FirstOrDefault();
                    if (placeholder != null)
                    {
                        var placeholderIndex = MediaItems.IndexOf(placeholder);
                        MediaItems[placeholderIndex] = vm;
                        _ = vm.LoadThumbnailAsync();
                    }
                });
            });

            // to ensure pagination is correct
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

        [RelayCommand]
        private async Task UploadFiles()
        {
            _logger.LogInformation("'Upload' button clicked.");

            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Upload aborted: Media folder path has not been set.");
                _messageBoxService.Show("No media folder set", "No media folder path has been set, please set it first before attempting to upload file(s).");
                return;
            }

            var selectedFiles = _fileDialogService.ShowDialog();
            if (selectedFiles == null || !selectedFiles.Any())
            {
                _logger.LogInformation("File selection was cancelled or no files were selected.");
                return;
            }

            var fileList = selectedFiles.ToList();
            _logger.LogInformation("{FileCount} file(s) have been selected for upload.", fileList.Count);

            var availableSlots = PageSize - MediaItems.Count;
            var placeholdersToAdd = Math.Min(fileList.Count, availableSlots);

            if (placeholdersToAdd > 0)
            {
                for (int i = 0; i < placeholdersToAdd; i++)
                {
                    MediaItems.Add(new PlaceholderViewModel());
                }
            }

            await Task.Run(async () =>
            {
                foreach (var sourcePath in fileList)
                {
                    try
                    {
                        var pathMappings = await _fileService.CopyFilesAsync(new[] { sourcePath }, mediaFolderPath);
                        if (!pathMappings.TryGetValue(sourcePath, out var newFilePath))
                        {
                            continue;
                        }

                        var mediaType = await _fileTypeIdentifierService.IdentifyAsync(newFilePath);
                        if (mediaType == MediaType.Unknown)
                        {
                            _logger.LogInformation("Skipping unsupported file {File}", newFilePath);
                            continue;
                        }

                        TimeSpan duration = TimeSpan.Zero;
                        if (mediaType == MediaType.Video)
                        {
                            var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(newFilePath);
                            duration = mediaInfo.Duration;
                        }

                        var thumbnailPath = await _thumbnailService.CreateThumbnailAsync(newFilePath, mediaType);

                        var newItem = new MediaItem
                        {
                            FileName = Path.GetFileName(sourcePath),
                            FilePath = newFilePath,
                            FileType = mediaType,
                            ThumbnailPath = thumbnailPath,
                            Duration = duration
                        };

                        await _databaseService.AddMediaItemAsync(newItem);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var placeholder = MediaItems.OfType<PlaceholderViewModel>().FirstOrDefault();
                            if (placeholder != null)
                            {
                                var vm = _mediaItemViewModelFactory.Create(newItem);
                                var placeholderIndex = MediaItems.IndexOf(placeholder);
                                MediaItems[placeholderIndex] = vm;
                                _ = vm.LoadThumbnailAsync();
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process uploaded file '{Path}'", sourcePath);
                    }
                }
            });

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
    }
}
