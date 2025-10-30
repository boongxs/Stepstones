using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using stepstones.Services.Core;
using stepstones.Models;
using stepstones.Resources;

namespace stepstones.ViewModels
{
    public partial class EnlargeVideoViewModel : EnlargeMediaViewModelBase
    {
        private readonly ITranscodingService _transcodingService;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [ObservableProperty]
        private Uri? _playableFileUri;

        [ObservableProperty]
        private bool _isTranscoding;

        public double LayoutWidth { get; private set; }
        public double LayoutHeight { get; private set; }

        public EnlargeVideoViewModel(string filePath, 
                                     MediaType fileType, 
                                     int width, 
                                     int height, 
                                     ITranscodingService transcodingService)
            : base(filePath, fileType, width, height)
        {
            _transcodingService = transcodingService;
            _playableFileUri = new Uri(this.FilePath);

            CalculateLayoutDimensions();
            _ = LoadVideoAsync();
        }

        private void CalculateLayoutDimensions()
        {
            double minSize = (double)AppConstants.MinimumDisplaySize;
            double originalWidth = (double)this.OriginalWidth;
            double originalHeight = (double)this.OriginalHeight;

            // default to original size
            LayoutWidth = originalWidth;
            LayoutHeight = originalHeight;

            // handle bad data
            if (LayoutWidth <= 0 ||  LayoutHeight <= 0)
            {
                LayoutWidth = minSize;
                LayoutHeight = minSize;

                return;
            }

            double aspectRatio = originalWidth / originalHeight;

            // case1: only height < minSize
            if (originalHeight < minSize && originalWidth >= minSize)
            {
                LayoutHeight = minSize;
                LayoutWidth = minSize * aspectRatio;
            }

            // case2: only width < minSize
            else if (originalWidth < minSize && originalHeight >= minSize)
            {
                LayoutWidth = minSize;
                LayoutHeight = minSize / aspectRatio;
            }

            // case3: both width and height < minSize
            else if (originalWidth < minSize && originalHeight < minSize)
            {
                if (originalWidth > originalHeight)
                {
                    LayoutHeight = minSize;
                    LayoutWidth = minSize * aspectRatio;
                }
                else
                {
                    LayoutWidth = minSize;
                    LayoutHeight = minSize / aspectRatio;
                }
            }

            //case4: both width and height > minSize
            // do nothing
        }

        private async Task LoadVideoAsync()
        {
            try
            {
                // check if transcoding is required
                if (await _transcodingService.IsTranscodingRequiredAsync(this.FilePath))
                {
                    IsTranscoding = true;

                    Log.Information("Transcoding required for '{Path}'. Starting...", this.FilePath);

                    var transcodedPath = await _transcodingService.EnsurePlayableFileAsync(this.FilePath, _cts.Token);
                    PlayableFileUri = new Uri(transcodedPath);

                    Log.Information("Transcoding complete for '{Path}'.", this.FilePath);
                }
                else
                {
                    // transcoding not required
                    Log.Information("No transcoding required for '{Path}'.", this.FilePath);
                }
            }
            catch (OperationCanceledException)
            {
                // user closed the dialog during transcoding
                Log.Information("Video preparation was cancelled for '{Path}'.", this.FilePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to prepare '{Path}' for playback.", this.FilePath);
                // toast to show transcoding failed?
            }
            finally
            {
                IsTranscoding = false;
            }
        }

        public void Cancel()
        {
            Log.Warning("Cancelling video preparation for '{Path}'.", this.FilePath);
            _cts.Cancel();
        }
    }
}
