using Microsoft.Extensions.Logging;
using System.IO;
using CommunityToolkit.Mvvm.Messaging;
using stepstones.Models;
using stepstones.Messages;
using stepstones.Enums;

namespace stepstones.Services.Core
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly IMessenger _messenger;

        public FileService(ILogger<FileService> logger,
                           IMessenger messenger)
        {
            _logger = logger;
            _messenger = messenger;
        }

        public async Task<Dictionary<string, string>> CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath)
        {
            var pathMappings = new Dictionary<string, string>();

            await Task.Run(() =>
            {
                foreach (var sourcePath in sourceFilePaths)
                {
                    try
                    {
                        var uniqueFileName = FileNameGenerator.GenerateUniqueFileName(sourcePath);
                        var destinationPath = Path.Combine(destinationFolderPath, uniqueFileName);

                        if (!File.Exists(destinationPath))
                        {
                            File.Copy(sourcePath, destinationPath);
                            _logger.LogInformation("Successfully copied '{SourceFile}' to '{DestinationFile}'", sourcePath, destinationPath);
                        }
                        else
                        {
                            _logger.LogInformation("File '{SourceFile}' already exists as '{DestinationFile}'. Skipping.", sourcePath, destinationPath);
                            _messenger.Send(new ShowToastMessage($"'{Path.GetFileName(sourcePath)}' already in media folder. Skipped.", ToastNotificationType.Info));
                        }

                        pathMappings[sourcePath] = destinationPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to copy '{SourceFile}'", sourcePath);
                    }
                }
            });

            return pathMappings;
        }

        public IEnumerable<string> GetAllFiles(string folderPath)
        {
            try
            {
                if (Directory.Exists(folderPath))
                {
                    return Directory.EnumerateFiles(folderPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enumerate files in folder '{FolderPath}'", folderPath);
            }
            return Enumerable.Empty<string>();
        }

        public void DeleteMediaFile(MediaItem item)
        {
            // delete the media file
            try
            {
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                    _logger.LogInformation("Successfully deleted media file '{Path}'", item.FilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete media file '{Path}'", item.FilePath);
                throw; //rethrow to notify of failure
            }

            // delete media file's thumbnail
            try
            {
                if (!string.IsNullOrWhiteSpace(item.ThumbnailPath) && File.Exists(item.ThumbnailPath))
                {
                    File.Delete(item.ThumbnailPath);
                    _logger.LogInformation("Successfully deleted thumbnail file '{Path}'", item.ThumbnailPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete thumbnail file '{Path}'", item.ThumbnailPath);
                throw; //rethrow to notify of failure
            }
        }
    }
}
