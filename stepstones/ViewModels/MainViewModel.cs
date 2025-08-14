using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using stepstones.Messages;
using stepstones.Models;
using stepstones.Services;

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
        private readonly IThumbnailService _thumbnailService;
        private readonly IMediaItemViewModelFactory _mediaItemViewModelFactory;
        private readonly IMessenger _messenger;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;

        public ObservableCollection<MediaItemViewModel> MediaItems { get; } = new();

        [ObservableProperty]
        private double _thumbnailWidth = 270;

        [ObservableProperty]
        private int _gridColumns = 4;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverlayVisible))]
        private object? _activeDialogViewModel;

        public bool IsOverlayVisible => _activeDialogViewModel != null;

        private RequestEditTagsMessage? _pendingEditTagsRequest;

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
                MediaItems.Remove(message.Value);
                _logger.LogInformation("Removed deleted item from UI: '{FileName}'", message.Value.FileName);
            });

            _messenger.Register<ShowDialogMessage>(this, (recipient, message) =>
            {
                ActiveDialogViewModel = message.ViewModel;
            });

            _messenger.Register<RequestEditTagsMessage>(this, (recipient, message) =>
            {
                _pendingEditTagsRequest = message;
                var dialogViewModel = new EditTagsViewModel(message.InitialTags);
                ActiveDialogViewModel = dialogViewModel;
            });

            logger.LogInformation("MainViewModel has been created.");

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var savedPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                _logger.LogInformation("Application startup: No previously saved media folder path found.");
            }
            else
            {
                _logger.LogInformation("Application startup: Located saved media folder, '{Path}'", savedPath);
            }

            await _synchronizationService.SynchronizeDataAsync(savedPath);
            await LoadMediaItemsAsync();
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

            _logger.LogInformation("Loading media items from database for folder '{Path}'", mediaFolderPath);
            var items = await _databaseService.GetAllItemsForFolderAsync(mediaFolderPath);

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
                _logger.LogInformation("User selected folder: {Path}", selectedPath);
                _settingsService.SaveMediaFolderPath(selectedPath);

                await _synchronizationService.SynchronizeDataAsync(selectedPath);
                await LoadMediaItemsAsync();
            }
        }

        [RelayCommand]
        private async Task UploadFiles()
        {
            _logger.LogInformation("'Upload' button clicked.");

            var mediaFolderPath = _settingsService.LoadMediaFolderPath();
            if (string.IsNullOrWhiteSpace(mediaFolderPath))
            {
                _logger.LogWarning("Upload aborted: Media folder path has not been set.");
                _messageBoxService.Show("No media folder path has been set, please set it first before attempting to upload file(s).");
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

            await _fileService.CopyFilesAsync(fileList, mediaFolderPath);

            foreach (var sourcePath in fileList)
            {
                var mediaType = await _fileTypeIdentifierService.IdentifyAsync(sourcePath);
                if (mediaType == MediaType.Unknown)
                {
                    _logger.LogInformation("Skipping unsupported file '{File}'", sourcePath);
                    continue;
                }

                var thumbnailPath = await _thumbnailService.CreateThumbnailAsync(sourcePath, mediaType);

                var newItem = new MediaItem
                {
                    FileName = Path.GetFileName(sourcePath),
                    FilePath = Path.Combine(mediaFolderPath, Path.GetFileName(sourcePath)),
                    FileType = mediaType,
                    ThumbnailPath = thumbnailPath
                };

                await _databaseService.AddMediaItemAsync(newItem);
                MediaItems.Add(_mediaItemViewModelFactory.Create(newItem));
            }
        }

        [RelayCommand]
        private void CloseDialog(string? result)
        {
            object? dialogResult = null;

            if (ActiveDialogViewModel is EditTagsViewModel editTagsVM)
            {
                var wasSaved = result == "Save";
                if (wasSaved)
                {
                    editTagsVM.Save();
                }

                dialogResult = new EditTagsResult { WasSaved = wasSaved, NewTags = editTagsVM.Result };
            }

            ActiveDialogViewModel = null;

            _messenger.Send(new DialogClosedMessage(dialogResult));
        }
    }
}
