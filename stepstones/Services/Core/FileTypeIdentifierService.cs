using FFMpegCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading;
using stepstones.Models;
using static stepstones.Resources.AppConstants;

namespace stepstones.Services.Core
{
    public class FileTypeIdentifierService : IFileTypeIdentifierService
    {
        private readonly ILogger<FileTypeIdentifierService> _logger;
        private const int FileReadyTimeoutMs = 30000;
        private const int FileReadyCheckIntervalMs = 500;

        public FileTypeIdentifierService(ILogger<FileTypeIdentifierService> logger)
        {
            _logger = logger;
        }

        private async Task<bool> IsFileReadyAsync(string filePath)
        {
            var cancellationTokenSource = new CancellationTokenSource(FileReadyTimeoutMs);

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    await Task.Delay(FileReadyCheckIntervalMs, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            return false;
        }

        public async Task<MediaType> IdentifyAsync(string filePath)
        {
            if (!await IsFileReadyAsync(filePath))
            {
                _logger.LogWarning("File '{File}' was not ready for processing after {Timeout} seconds. Skipping.", filePath, FileReadyTimeoutMs / 1000);
                return MediaType.Unknown;
            }

            // if GIF
            if (Path.GetExtension(filePath).Equals(GifExtension, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Identified '{File}' as GIF.", filePath);
                return MediaType.Gif;
            }

            // if IMAGE
            try
            {
                var imageInfo = await Image.IdentifyAsync(filePath);
                if (imageInfo != null)
                {
                    _logger.LogInformation("Identified '{File}' as Image.", filePath);
                    return MediaType.Image;
                }
            }
            catch (Exception)
            {
                _logger.LogInformation("File '{File}' has not been identified as Image.", filePath);
            }

            // if VIDEO or AUDIO
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                if (mediaInfo.VideoStreams.Any())
                {
                    _logger.LogInformation("Identified '{File}' as Video.", filePath);
                    return MediaType.Video;
                }
                if (mediaInfo.AudioStreams.Any())
                {
                    _logger.LogInformation("Identified '{File}' as Audio.", filePath);
                    return MediaType.Audio;
                }
            }
            catch (Exception)
            {
                _logger.LogInformation("File '{Path}' has not been identified as Video.", filePath);
            }

            // if UNKNOWN
            _logger.LogWarning("Could not identify '{File}' as a Image or Video.", filePath);
            return MediaType.Unknown;
        }
    }
}
