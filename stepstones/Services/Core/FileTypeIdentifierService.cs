using FFMpegCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using stepstones.Models;
using System.IO;

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
            // if GIF
            if (Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase))
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
