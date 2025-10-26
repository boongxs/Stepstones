using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using stepstones.Services.Core;
using stepstones.Models;

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

        public EnlargeVideoViewModel(string filePath, 
                                     MediaType fileType, 
                                     int width, 
                                     int height, 
                                     ITranscodingService transcodingService)
            : base(filePath, fileType, width, height)
        {
            _transcodingService = transcodingService;
            _playableFileUri = new Uri(this.FilePath);
            _ = LoadVideoAsync();
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
