using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;
using stepstones.Services.Data;

namespace stepstones.Services.Core
{
    public class MediaItemProcessorService : IMediaItemProcessorService
    {
        private readonly ILogger<MediaItemProcessorService> _logger;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IDatabaseService _databaseService;

        public MediaItemProcessorService(ILogger<MediaItemProcessorService> logger, 
                                         IFileTypeIdentifierService fileTypeIdentifierService, 
                                         IThumbnailService thumbnailService, 
                                         IDatabaseService databaseService)
        {
            _logger = logger;
            _fileTypeIdentifierService = fileTypeIdentifierService;
            _thumbnailService = thumbnailService;
            _databaseService = databaseService;
        }

        public async Task<MediaItem?> ProcessNewFileAsync(string originalPath, string finalPath)
        {
            try
            {
                // identify the file type
                var mediaType = await _fileTypeIdentifierService.IdentifyAsync(finalPath);
                if (mediaType == MediaType.Unknown)
                {
                    _logger.LogInformation("Skipping unsupported file type for '{Path}'", finalPath);
                    return null;
                }

                // get the duration if it's a video file
                TimeSpan duration = TimeSpan.Zero;
                if (mediaType == MediaType.Video || mediaType == MediaType.Audio)
                {
                    var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(finalPath);
                    duration = mediaInfo.Duration;
                }

                // create the thumbnail
                string? thumbnailPath = null;
                if (mediaType != MediaType.Audio)
                {
                    thumbnailPath = await _thumbnailService.CreateThumbnailAsync(finalPath, mediaType);
                }
                else
                {
                    thumbnailPath = "pack://application:,,,/Resources/audio_placeholder.jpg";
                }

                // construct the MediaItem object
                var newItem = new MediaItem
                {
                    FileName = Path.GetFileName(originalPath),
                    FilePath = finalPath,
                    FileType = mediaType,
                    ThumbnailPath = thumbnailPath,
                    Duration = duration
                };

                // save to the database
                await _databaseService.AddMediaItemAsync(newItem);

                _logger.LogInformation("Successfully processed and saved new media item for '{FileName}'", newItem.FileName);
                return newItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process new file '{Path}'", finalPath);
                return null;
            }
        }
    }
}
