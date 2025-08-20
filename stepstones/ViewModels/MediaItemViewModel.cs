using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using stepstones.Messages;
using stepstones.Models;
using System.Windows.Media.Imaging;
using System.IO;
using stepstones.Services.Interaction;
using stepstones.Services.Data;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;

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
        private readonly IDialogPresenter _dialogPresenter;

        [ObservableProperty]
        private BitmapImage? _thumbnailImage;

        public MediaItemViewModel(MediaItem mediaItem, 
                                  IClipboardService clipboardService, 
                                  IMessageBoxService messageBoxService, 
                                  IFileService fileService, 
                                  IDatabaseService databaseService, 
                                  IMessenger messenger,
                                  IImageDimensionService imageDimensionService,
                                  IDialogPresenter dialogPresenter)
        {
            _mediaItem = mediaItem;

            _clipboardService = clipboardService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _messenger = messenger;
            _imageDimensionService = imageDimensionService;
            _dialogPresenter = dialogPresenter;
        }

        public string FileName => _mediaItem.FileName;
        public string FilePath => _mediaItem.FilePath;
        public MediaType FileType => _mediaItem.FileType;
        public string? ThumbnailPath => _mediaItem.ThumbnailPath;

        public async Task LoadThumbnailAsync()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailPath) || !File.Exists(ThumbnailPath))
            {
                return;
            }

            try
            {
                var bitmap = await Task.Run(() =>
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(ThumbnailPath);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                });

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
        private async Task Tags()
        {
            var originalTags = _mediaItem.Tags;
            var result = await _dialogPresenter.ShowEditTagsDialogAsync(originalTags);

            if (result.WasSaved)
            {
                var newTags = result.NewTags?.Trim();

                if (originalTags != newTags)
                {
                    _mediaItem.Tags = newTags;
                    await _databaseService.UpdateMediaItemAsync(_mediaItem);
                }
            }
        }

        [RelayCommand]
        private async Task Enlarge()
        {
            var dimensions = await _imageDimensionService.GetDimensionsAsync(this.FilePath, this.FileType);
            if (dimensions.Width == 0 || dimensions.Height == 0)
            {
                return;
            }

            BitmapImage? loadedImage = null;

            if (this.FileType == MediaType.Image)
            {
                loadedImage = new BitmapImage();
                loadedImage.BeginInit();
                loadedImage.UriSource = new Uri(this.FilePath);
                loadedImage.CacheOption = BitmapCacheOption.OnLoad;
                loadedImage.EndInit();
                loadedImage.Freeze();
            }

            var dialogViewModel = new EnlargeMediaViewModel(
                this.FilePath,
                this.FileType,
                dimensions.Width,
                dimensions.Height,
                loadedImage);

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
