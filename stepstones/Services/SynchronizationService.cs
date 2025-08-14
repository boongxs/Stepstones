using Microsoft.Extensions.Logging;
using System.IO;
using stepstones.Models;

namespace stepstones.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly ILogger<SynchronizationService> _logger;
        private readonly IDatabaseService _databaseService;
        private readonly IFileService _fileService;

        public SynchronizationService(ILogger<SynchronizationService> logger,
                                      IDatabaseService databaseService,
                                      IFileService fileService)
        {
            _logger = logger;
            _databaseService = databaseService;
            _fileService = fileService;
        }

        public async Task SynchronizeDataAsync(string folderPath)
        {
            var filesInFolder = _fileService.GetAllFiles(folderPath).ToList();
            var itemsInDatabase = await _databaseService.GetAllItemsForFolderAsync(folderPath);
            var filePathsInDatabase = itemsInDatabase.Select(i => i.FilePath).ToList();

            var ghosts = filePathsInDatabase.Except(filesInFolder).ToList();
            if (ghosts.Any())
            {
                _logger.LogInformation("Found {Count} ghost records to delete from the database.", ghosts.Count);
                await _databaseService.DeleteItemsByPathsAsync(ghosts);
            }

            var orphans = filesInFolder.Except(filePathsInDatabase).ToList();
            if (orphans.Any())
            {
                _logger.LogInformation("Found {Count} orphan files to import into the database.", orphans.Count);
                foreach (var orphanPath in orphans)
                {
                    var newItem = new MediaItem
                    {
                        FileName = Path.GetFileName(orphanPath),
                        FilePath = orphanPath,
                        FileType = Path.GetExtension(orphanPath).ToLowerInvariant()
                    };
                    await _databaseService.AddMediaItemAsync(newItem);
                }
            }
        }
    }
}
