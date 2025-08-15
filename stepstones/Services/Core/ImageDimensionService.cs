using FFMpegCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using stepstones.Models;

namespace stepstones.Services.Core
{
    public class ImageDimensionService : IImageDimensionService
    {
        private readonly ILogger<ImageDimensionService> _logger;

        public ImageDimensionService(ILogger<ImageDimensionService> logger)
        {
            _logger = logger;
        }

        public async Task<(int Width, int Height)> GetDimensionsAsync(string filePath, MediaType mediaType)
        {
            try
            {
                switch (mediaType)
                {
                    case MediaType.Image:
                        var imageInfo = await Image.IdentifyAsync(filePath);
                        if (imageInfo != null)
                        {
                            return (imageInfo.Width, imageInfo.Height);
                        }
                        break;

                    case MediaType.Video:
                        var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                        var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                        if (videoStream != null)
                        {
                            return (videoStream.Width, videoStream.Height);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dimensions for file '{Path}'", filePath);
            }

            return (0, 0);
        }
    }
}
