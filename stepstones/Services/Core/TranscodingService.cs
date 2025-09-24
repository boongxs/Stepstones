using FFMpegCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace stepstones.Services.Core
{
    public class TranscodingService : ITranscodingService
    {
        private readonly ILogger<TranscodingService> _logger;
        private readonly string _transcodeCacheFolder;

        public TranscodingService(ILogger<TranscodingService> logger)
        {
            _logger = logger;
            var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "stepstones");
            _transcodeCacheFolder = Path.Combine(appDataFolder, "transcode-cache");
            Directory.CreateDirectory(_transcodeCacheFolder);
        }

        public async Task<bool> IsTranscodingRequiredAsync(string filePath)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                // valid codec case
                if (videoStream != null && videoStream.CodecName == "h264")
                {
                    return false;
                }

                // found in cache case
                var outputFilePath = GetCachePathForFile(filePath);
                if (File.Exists(outputFilePath))
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to determine if transcoding is required for '{Path}'. Assuming false.", filePath);
                return false;
            }
        }

        public async Task<string> EnsurePlayableFileAsync(string filePath)
        {
            var outputFilePath = GetCachePathForFile(filePath);

            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();

                if (videoStream != null && videoStream.CodecName == "h264")
                {
                    _logger.LogInformation("Video '{Path}' is already in a compatible format. Playing directly.", filePath);
                    return filePath;
                }

                if (File.Exists(outputFilePath))
                {
                    _logger.LogInformation("Found transcoded file in cache.");
                    return outputFilePath;
                }

                _logger.LogInformation("Video '{Path}' has an incompatible codec ('{Codec}'). Starting transcode.", filePath, videoStream.CodecName);

                await FFMpegArguments
                    .FromFileInput(filePath)
                    .OutputToFile(outputFilePath, true, options => options
                        .WithVideoCodec("libx264")
                        .WithAudioCodec("aac"))
                    .ProcessAsynchronously();

                _logger.LogInformation("Successfully transcoded file to '{Path}'", outputFilePath);
                return outputFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to transcode file '{Path}'. Returning original path as fallback.", filePath);
                return filePath;
            }
        }

        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_transcodeCacheFolder))
                {
                    Directory.Delete(_transcodeCacheFolder, true);
                    _logger.LogInformation("Cleared transcoding cache.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear transcoding cache.");
            }
        }

        public string GetCachePathForFile(string sourceFilePath)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(sourceFilePath.ToLowerInvariant()));
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return Path.Combine(_transcodeCacheFolder, $"{hashString}.mp4");
        }
    }
}
