using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.IO;
using System.Threading.Tasks;
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

        public BitmapImage? ThumbnailImage { get; private set; }

        public MediaItemViewModel(MediaItem mediaItem,
                                  IClipboardService clipboardService,
                                  IMessageBoxService messageBoxService,
                                  IFileService fileService,
                                  IDatabaseService databaseService,
                                  IMessenger messenger)
        {
            _mediaItem = mediaItem;
            _clipboardService = clipboardService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _messenger = messenger;

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
            catch (Exception ex)
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
