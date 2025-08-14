using FFMpegCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace stepstones.Services
{
    public class ThumbnailService : IThumbnailService
    {
        private readonly ILogger<ThumbnailService> _logger;
        private readonly string _thumbnailCacheFolder;
        private const int ThumbnailSize = 250;

        public ThumbnailService(ILogger<ThumbnailService> logger)
        {
            _logger = logger;

            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "stepstones");
            _thumbnailCacheFolder = Path.Combine(appDataFolder, "thumbnails");
            Directory.CreateDirectory(_thumbnailCacheFolder);
        }

        public async Task<string?> CreateThumbnailAsync(string sourceFilePath)
        {
            var tempImagePath = string.Empty;

            try
            {
                var cachePath = GetCachePath(sourceFilePath);

                if (File.Exists(cachePath))
                {
                    _logger.LogInformation("Thumbnail found in cache for {SourceFile}", sourceFilePath);
                    return cachePath;
                }

                Image? sourceImage = null;

                try
                {
                    sourceImage = await Image.LoadAsync(sourceFilePath);
                    _logger.LogInformation("'{SourceFile}' identified as an image.", sourceFilePath);
                }
                catch (UnknownImageFormatException)
                {
                    _logger.LogInformation("'{SourceFile}' is not a supported image. Attempting to process as video.", sourceFilePath);
                    tempImagePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.png");
                    var success = await FFMpeg.SnapshotAsync(sourceFilePath, tempImagePath, new System.Drawing.Size(1920, 1080), TimeSpan.FromSeconds(1));

                    if (success)
                    {
                        sourceImage = await Image.LoadAsync(tempImagePath);
                        _logger.LogInformation("Successfully extracted frame from video '{SourceFile}'", sourceFilePath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to extract frame from video '{SourceFile}'", sourceFilePath);
                    }
                }

                if (sourceImage is null)
                {
                    _logger.LogError("Could not process file '{SourceFile}' as either image or video.", sourceFilePath);
                    return null;
                }

                using (sourceImage)
                {
                    ResizeAndCropImage(sourceImage);
                    await sourceImage.SaveAsJpegAsync(cachePath);
                    _logger.LogInformation("Successfully created and cached thumbnail for {SourceFile}", sourceFilePath);
                }

                return cachePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while creating thumbnail for '{SourceFile}'", sourceFilePath);
                return null;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempImagePath) && File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
        }

        private string GetCachePath(string sourceFilePath)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sourceFilePath.ToLowerInvariant()));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(_thumbnailCacheFolder, $"{hashString}.jpg");
        }

        private void ResizeAndCropImage(Image image)
        {
            var options = new ResizeOptions
            {
                Size = new Size(ThumbnailSize, ThumbnailSize),
                Mode = ResizeMode.Crop
            };
            image.Mutate(x => x.Resize(options));
        }
    }
}
