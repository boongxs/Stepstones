using FFMpegCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using stepstones.Models;

namespace stepstones.Services.Core
{
    public class FileTypeIdentifierService : IFileTypeIdentifierService
    {
        private readonly ILogger<FileTypeIdentifierService> _logger;

        public FileTypeIdentifierService(ILogger<FileTypeIdentifierService> logger)
        {
            _logger = logger;
        }

        public async Task<MediaType> IdentifyAsync(string filePath)
        {
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

            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                if (mediaInfo.VideoStreams.Any())
                {
                    _logger.LogInformation("Identified '{File}' as Video.", filePath);
                    return MediaType.Video;
                }
            }
            catch (Exception)
            {
                _logger.LogInformation("File '{Path}' has not been identified as Video.", filePath);
            }

            _logger.LogWarning("Could not identify '{File}' as a Image or Video.", filePath);
            return MediaType.Unknown;
        }
    }
}
