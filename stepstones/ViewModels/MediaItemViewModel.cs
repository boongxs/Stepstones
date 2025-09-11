using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows.Media.Imaging;
using System.IO;
using Serilog;
using stepstones.Services.Interaction;
using stepstones.Services.Data;
using stepstones.Services.Core;
using stepstones.Services.Infrastructure;
using stepstones.Enums;
using stepstones.Messages;
using stepstones.Models;

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
        public bool IsVideo => FileType == MediaType.Video;
        public bool IsGif => FileType == MediaType.Gif;
        public string FormattedDuration => _mediaItem.Duration.ToString(@"hh\:mm\:ss");

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
            try
            {
                _clipboardService.CopyFileToClipboard(FilePath);
                _messenger.Send(new ShowToastMessage("File copied to clipboard.", ToastNotificationType.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while copying '{File}' to the clipboard.", FileName);
                _messenger.Send(new ShowToastMessage("Failed to copy file.", ToastNotificationType.Error));
            }
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
                    try
                    {
                        _mediaItem.Tags = newTags;
                        await _databaseService.UpdateMediaItemAsync(_mediaItem);
                        _messenger.Send(new ShowToastMessage("Tags updated successfully.", ToastNotificationType.Success));
                    }
                    catch (Exception ex)
                    {
                        _mediaItem.Tags = originalTags;
                        Log.Error(ex, "Failed to update tags for '{FileName}'.", FileName);
                        _messenger.Send(new ShowToastMessage("Failed to save tags.", ToastNotificationType.Error));
                    }
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

            object? dialogViewModel = null;

            switch (this.FileType)
            {
                case MediaType.Image:
                    BitmapImage? loadedImage = new BitmapImage();
                    loadedImage.BeginInit();
                    loadedImage.UriSource = new Uri(this.FilePath);
                    loadedImage.CacheOption = BitmapCacheOption.OnLoad;
                    loadedImage.EndInit();
                    loadedImage.Freeze();

                    dialogViewModel = new EnlargeImageViewModel(
                        this.FilePath,
                        this.FileType,
                        dimensions.Width,
                        dimensions.Height,
                        loadedImage);

                    break;

                case MediaType.Video:
                    dialogViewModel = new EnlargeVideoViewModel(
                        this.FilePath,
                        this.FileType,
                        dimensions.Width,
                        dimensions.Height);

                    break;

                case MediaType.Gif:
                    dialogViewModel = new EnlargeGifViewModel(
                        this.FilePath,
                        this.FileType,
                        dimensions.Width,
                        dimensions.Height);

                    break;
            }

            if (dialogViewModel != null)
            {
                _messenger.Send(new ShowDialogMessage(dialogViewModel));
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            bool confirmed = _messageBoxService.ShowConfirmation("Delete File", $"Are you sure you want to permanently delete '{FileName}'?");

            if (confirmed)
            {
                try
                {
                    ThumbnailImage = null;
                    OnPropertyChanged(nameof(ThumbnailImage));

                    _fileService.DeleteMediaFile(_mediaItem);
                    await _databaseService.DeleteMediaItemAsync(_mediaItem);

                    _messenger.Send(new MediaItemDeletedMessage(this));
                    _messenger.Send(new ShowToastMessage($"'{_mediaItem.FileName}' was deleted.", ToastNotificationType.Success));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred during the deletion of '{FileName}'.", _mediaItem.FileName);
                    _messenger.Send(new ShowToastMessage($"Failed to delete '{_mediaItem.FileName}'.", ToastNotificationType.Error));
                    await LoadThumbnailAsync();
                }
            }
            else
            {
                return;
            }
        }
    }
}
