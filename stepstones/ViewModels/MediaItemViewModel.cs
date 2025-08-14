using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using stepstones.Models;
using stepstones.Services;
using stepstones.Messages;

namespace stepstones.ViewModels
{
    public partial class MediaItemViewModel : ObservableObject
    {
        private readonly MediaItem _mediaItem;
        private readonly IClipboardService _clipboardService;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IFileService _fileService;
        private readonly IDatabaseService _databaseService;
        private readonly IMessenger _messenger;
        private readonly IImageDimensionService _imageDimensionService;

        public BitmapImage? ThumbnailImage { get; private set; }

        public MediaItemViewModel(MediaItem mediaItem,
                                  IClipboardService clipboardService,
                                  IMessageBoxService messageBoxService,
                                  IFileService fileService,
                                  IDatabaseService databaseService,
                                  IMessenger messenger,
                                  IImageDimensionService imageDimensionService)
        {
            _mediaItem = mediaItem;
            _clipboardService = clipboardService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _messenger = messenger;
            _imageDimensionService = imageDimensionService;

            LoadThumbnail();
        }

        public string FileName => _mediaItem.FileName;
        public string FilePath => _mediaItem.FilePath;
        public MediaType FileType => _mediaItem.FileType;
        public string? ThumbnailPath => _mediaItem.ThumbnailPath;

        private void LoadThumbnail()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailPath) || !File.Exists(ThumbnailPath))
            {
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(ThumbnailPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ThumbnailImage = bitmap;
            }
            catch (Exception)
            {
                ThumbnailImage = null;
            }
        }

        [RelayCommand]
        private void Copy()
        {
            _clipboardService.CopyFileToClipboard(FilePath);
        }

        [RelayCommand]
        private async Task Enlarge()
        {
            var dimensions = await _imageDimensionService.GetDimensionsAsync(this.FilePath, this.FileType);
            if (dimensions.Width == 0 || dimensions.Height == 0)
            {
                return;
            }

            var dialogViewModel = new EnlargeMediaViewModel(this.FilePath,
                                                            this.FileType,
                                                            dimensions.Width,
                                                            dimensions.Height);

            _messenger.Send(new ShowDialogMessage(dialogViewModel));
        }

        [RelayCommand]
        private async Task Delete()
        {
            bool confirmed = _messageBoxService.ShowConfirmation("Delete File", $"Are you sure you want to permanently delete '{FileName}'?");

            if (confirmed)
            {
                ThumbnailImage = null;
                OnPropertyChanged(nameof(ThumbnailImage));

                _fileService.DeleteMediaFile(_mediaItem);
                await _databaseService.DeleteMediaItemAsync(_mediaItem);

                _messenger.Send(new MediaItemDeletedMessage(this));
            }
            else
            {
                return;
            }
        }
    }
}
