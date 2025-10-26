using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;
using stepstones.Services.Core;
using static stepstones.Resources.AppConstants;
using stepstones.Resources;

namespace stepstones.Services.Data
{
    public class DataMigrationService : IDataMigrationService
    {
        private readonly ILogger<DataMigrationService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IThumbnailService _thumbnailService;
        private readonly IImageDimensionService _imageDimensionsService;

        public DataMigrationService(ILogger<DataMigrationService> logger, 
                                    IDatabaseService databaseService,
                                    IThumbnailService thumbnailService,
                                    IImageDimensionService imageDimensionService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _thumbnailService = thumbnailService;
            _imageDimensionsService = imageDimensionService;
        }

        public void RunMigration(string folderPath, Action<MediaItem> onItemRepaired)
        {
            Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Starting background data migration check for folder '{Path}'.", folderPath);

                    var itemsInFolder = await _databaseService.GetAllItemsForFolderAsync(folderPath);

                    await CheckDurationsAsync(itemsInFolder, onItemRepaired);
                    await CheckThumbnailPathsAsync(itemsInFolder, onItemRepaired);
                    await CheckDimensionsAsync(itemsInFolder, onItemRepaired);

                    _logger.LogInformation("Background data migration check completed for '{Path}'.", folderPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during the background data migration for folder '{Path}'.", folderPath);
                }
            });
        }

        private async Task CheckDurationsAsync(List<MediaItem> items, Action<MediaItem> onItemRepaired)
        {
            var videos = items
                .Where(item => item.FileType == MediaType.Video && item.Duration == TimeSpan.Zero)
                .ToList();

            if (!videos.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} video records with missing Duration value.", videos.Count);

            foreach (var video in videos)
            {
                try
                {
                    var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(video.FilePath);
                    video.Duration = mediaInfo.Duration;

                    await _databaseService.UpdateMediaItemAsync(video);

                    onItemRepaired(video);

                    _logger.LogInformation("Successfully updated duration for '{FileName}'.", video.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update duration for file '{FilePath}'. Skipping.", video.FilePath);
                }
            }
        }

        private async Task CheckThumbnailPathsAsync(List<MediaItem> items, Action<MediaItem> onItemRepaired)
        {
            var itemsWithMissingThumbnails = items
                .Where(item =>
                {
                    if (item.FileType == MediaType.Audio)
                    {
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(item.ThumbnailPath))
                    {
                        return true;
                    }

                    return !File.Exists(item.ThumbnailPath);
                })
                .ToList();

            if (!itemsWithMissingThumbnails.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} records with missing thumbnails.", itemsWithMissingThumbnails.Count);

            foreach (var item in itemsWithMissingThumbnails)
            {
                try
                {
                    var newThumbnailPath = await _thumbnailService.CreateThumbnailAsync(item.FilePath, item.FileType);
                    item.ThumbnailPath = newThumbnailPath;

                    await _databaseService.UpdateMediaItemAsync(item);

                    onItemRepaired(item);

                    _logger.LogInformation("Successfully updated thumbnail path for '{FileName}'.", item.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to regenerate for '{FilePath}'. Skipping.", item.FilePath);
                }
            }
        }

        private async Task CheckDimensionsAsync(List<MediaItem> items, Action<MediaItem> onItemRepaired)
        {
            // find items where width or height is 0
            var itemsWithMissingDimensions = items
                .Where(item => item.Width == 0 || item.Height == 0)
                .ToList();

            if (!itemsWithMissingDimensions.Any())
            {
                return;
            }

            _logger.LogInformation("Found {Count} records with missing dimensions.", itemsWithMissingDimensions.Count);

            foreach (var item in itemsWithMissingDimensions)
            {
                try
                {
                    if (item.FileType == MediaType.Audio)
                    {
                        item.Width = AppConstants.MinimumDisplaySize;
                        item.Height = AppConstants.MinimumDisplaySize;
                    }
                    else
                    {
                        var dimensions = await _imageDimensionsService.GetDimensionsAsync(item.FilePath, item.FileType);
                        item.Width = dimensions.Width;
                        item.Height = dimensions.Height;
                    }

                    await _databaseService.UpdateMediaItemAsync(item);
                    onItemRepaired(item);
                    _logger.LogInformation("Successfully updated dimensions for '{FileName}'.", item.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update dimensions for '{FilePath}'. Skipping", item.FilePath);
                }
            }
        }
    }
}
