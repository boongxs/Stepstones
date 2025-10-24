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
using static stepstones.Resources.AppConstants;

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
        private readonly IDialogService _dialogService;
        private readonly ITranscodingService _transcodingService;

        private CancellationTokenSource? _transcodingCts;

        [ObservableProperty]
        private BitmapImage? _thumbnailImage;

        public MediaItemViewModel(MediaItem mediaItem, 
                                  IClipboardService clipboardService, 
                                  IMessageBoxService messageBoxService, 
                                  IFileService fileService, 
                                  IDatabaseService databaseService, 
                                  IMessenger messenger,
                                  IImageDimensionService imageDimensionService,
                                  IDialogService dialogService,
                                  ITranscodingService transcodingService)
        {
            _mediaItem = mediaItem;

            _clipboardService = clipboardService;
            _messageBoxService = messageBoxService;
            _fileService = fileService;
            _databaseService = databaseService;
            _messenger = messenger;
            _imageDimensionService = imageDimensionService;
            _dialogService = dialogService;
            _transcodingService = transcodingService;
        }

        public string FileName => _mediaItem.FileName;
        public string FilePath => _mediaItem.FilePath;
        public MediaType FileType => _mediaItem.FileType;
        public string? ThumbnailPath => _mediaItem.ThumbnailPath;
        public bool IsVideo => FileType == MediaType.Video;
        public bool IsGif => FileType == MediaType.Gif;
        public bool IsAudio => FileType == MediaType.Audio;
        public string FormattedDuration => _mediaItem.Duration.ToString(@"hh\:mm\:ss");

        public string? MediaTypeOverlayText =>
            IsVideo ? FormattedDuration :
            IsGif ? "GIF" :
            IsAudio ? "AUDIO" :
            null;

        public async Task LoadThumbnailAsync()
        {
            if (string.IsNullOrWhiteSpace(ThumbnailPath))
            {
                return;
            }

            if (!ThumbnailPath.StartsWith("pack://"))
            {
                if (!File.Exists(ThumbnailPath))
                {
                    return;
                }
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
                _messenger.Send(new ShowToastMessage(FileCopiedSuccessMessage, ToastNotificationType.Success));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while copying '{File}' to the clipboard.", FileName);
                _messenger.Send(new ShowToastMessage(FileCopyErrorMessage, ToastNotificationType.Error));
            }
        }

        [RelayCommand]
        private async Task Tags()
        {
            // get the selected item's Tags column value and pre-fill the dialog
            var originalTags = _mediaItem.Tags;
            var result = await _dialogService.ShowEditTagsDialogAsync(originalTags);

            if (result.WasSaved)
            {
                // get rid of leading/trailing spaces
                var newTags = result.NewTags?.Trim();

                if (originalTags != newTags)
                {
                    try
                    {
                        _mediaItem.Tags = newTags;
                        await _databaseService.UpdateMediaItemAsync(_mediaItem);
                        _messenger.Send(new ShowToastMessage(TagsUpdateSuccessMessage, ToastNotificationType.Success));
                    }
                    catch (Exception ex)
                    {
                        _mediaItem.Tags = originalTags;
                        Log.Error(ex, "Failed to update tags for '{FileName}'.", FileName);
                        _messenger.Send(new ShowToastMessage(TagsUpdateErrorMessage, ToastNotificationType.Error));
                    }
                }
            }
        }

        [RelayCommand]
        private async Task Enlarge()
        {
            var dimensions = (Width: 0, Height: 0);
            if (this.FileType != MediaType.Audio)
            {
                dimensions = await _imageDimensionService.GetDimensionsAsync(this.FilePath, this.FileType);
                if (dimensions.Width == 0 || dimensions.Height == 0)
                {
                    return;
                }
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
                    _transcodingCts = new CancellationTokenSource();

                    if (await _transcodingService.IsTranscodingRequiredAsync(this.FilePath))
                    {
                        _dialogService.ShowTranscodingDialog(_transcodingCts);
                    }

                    try
                    {
                        var playableFilePath = await _transcodingService.EnsurePlayableFileAsync(this.FilePath, _transcodingCts.Token);

                        if (!_transcodingCts.Token.IsCancellationRequested)
                        {
                            dialogViewModel = new EnlargeVideoViewModel(
                                playableFilePath,
                                this.FileType,
                                dimensions.Width,
                                dimensions.Height);

                            _dialogService.ShowDialog(dialogViewModel);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Log.Information("Cancellation was requested. Cleaning up transcoded file.");
                        var cachedFilePath = _transcodingService.GetCachePathForFile(this.FilePath);
                        try
                        {
                            if (File.Exists(cachedFilePath))
                            {
                                File.Delete(cachedFilePath);
                                Log.Information("Successfully deleted transcoded file '{Path}'", cachedFilePath);

                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to delete transcoded file '{Path}'", cachedFilePath);
                        }
                    }

                    break;

                case MediaType.Gif:
                    dialogViewModel = new EnlargeGifViewModel(
                        this.FilePath,
                        this.FileType,
                        dimensions.Width,
                        dimensions.Height);

                    break;

                case MediaType.Audio:
                    dialogViewModel = new EnlargeAudioViewModel(
                        this.FilePath,
                        this.FileType,
                        MinimumDisplaySize,
                        MinimumDisplaySize);
                    break;
            }

            if (dialogViewModel != null)
            {
                _dialogService.ShowDialog(dialogViewModel);
            }
        }

        [RelayCommand]
        private async Task Delete()
        {
            var message = string.Format(DeleteFileConfirmationMessage, FileName);
            bool confirmed = _messageBoxService.ShowConfirmation(DeleteFileConfirmationTitle, message);

            if (confirmed)
            {
                try
                {
                    ThumbnailImage = null;
                    OnPropertyChanged(nameof(ThumbnailImage));

                    _fileService.DeleteMediaFile(_mediaItem);
                    await _databaseService.DeleteMediaItemAsync(_mediaItem);

                    _messenger.Send(new MediaItemDeletedMessage(this));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred during the deletion of '{FileName}'.", _mediaItem.FileName);

                    var toastMessage = string.Format(FileDeleteErrorMessage, _mediaItem.FileName);
                    _messenger.Send(new ShowToastMessage(toastMessage, ToastNotificationType.Error));

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
