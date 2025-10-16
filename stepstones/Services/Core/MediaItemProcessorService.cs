using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;
using stepstones.Services.Data;
using stepstones.Services.Infrastructure;

namespace stepstones.Services.Core
{
    public class MediaItemProcessorService : IMediaItemProcessorService
    {
        private readonly ILogger<MediaItemProcessorService> _logger;
        private readonly IFileTypeIdentifierService _fileTypeIdentifierService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;
        private readonly IFolderWatcherService _folderWatcherService;

        public MediaItemProcessorService(ILogger<MediaItemProcessorService> logger, 
                                         IFileTypeIdentifierService fileTypeIdentifierService, 
                                         IThumbnailService thumbnailService, 
                                         IDatabaseService databaseService,
                                         IFileService fileService,
                                         IFolderWatcherService folderWatcherService)
        {
            _logger = logger;
            _fileTypeIdentifierService = fileTypeIdentifierService;
            _thumbnailService = thumbnailService;
            _databaseService = databaseService;
            _fileService = fileService;
            _folderWatcherService = folderWatcherService;
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

        public async Task ProcessUploadedFilesAsync(IEnumerable<string> sourceFilePaths, string destinationPath, IProgress<string> progress)
        {
            _folderWatcherService.StopWatching();
            var fileList = sourceFilePaths.ToList();
            var processedCount = 0;

            try
            {
                foreach (var sourcePath in fileList)
                {
                    var newPath = await _fileService.CopyFileAsync(sourcePath, destinationPath);
                    if (!string.IsNullOrWhiteSpace(newPath))
                    {
                        await ProcessNewFileAsync(sourcePath, newPath);
                        processedCount++;
                    }

                    var currentFileNumber = fileList.IndexOf(sourcePath) + 1;
                    progress.Report($"Processing {currentFileNumber} of {fileList.Count} files...");
                }
            }
            finally
            {
                _folderWatcherService.StartWatching(destinationPath);
            }
        }

        public async Task ProcessOrphanFilesAsync(IEnumerable<string> orphanPaths, IProgress<string> progress)
        {
            var orphanList = orphanPaths.ToList();
            var processedCount = 0;
            _folderWatcherService.StopWatching();

            try
            {
                foreach (var orphanPath in orphanList)
                {
                    var uniqueFileName = FileNameGenerator.GenerateUniqueFileName(orphanPath);
                    var newPath = Path.Combine(Path.GetDirectoryName(orphanPath), uniqueFileName);
                    File.Move(orphanPath, newPath);

                    await ProcessNewFileAsync(orphanPath, newPath);
                    processedCount++;
                    progress.Report($"Processing {processedCount} of {orphanList.Count} orphan files...");
                }
            }
            finally
            {
                _folderWatcherService.StartWatching(Path.GetDirectoryName(orphanList.FirstOrDefault()));
            }
        }
    }
}
