using Microsoft.Extensions.Logging;
using SQLite;
using System.IO;
using stepstones.Models;

namespace stepstones.Services.Data
{
    public class DatabaseService : IDatabaseService
    {
        private readonly ILogger<DatabaseService> _logger;
        private SQLiteAsyncConnection? _database;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
        }

        private async Task InitAsync()
        {
            if (_database is not null)
            {
                return;
            }

            try
            {
                var appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "stepstones");
                Directory.CreateDirectory(appDataFolder);
                var databasePath = Path.Combine(appDataFolder, "stepstones.db");

                _database = new SQLiteAsyncConnection(databasePath);
                await _database.CreateTableAsync<MediaItem>();
                _logger.LogInformation("Database initialized at '{Path}'", databasePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database.");
            }
        }

        public async Task AddMediaItemAsync(MediaItem item)
        {
            await InitAsync();
            if (_database is null)
            {
                _logger.LogError("Database is not initialized, cannot add media item.");
                return;
            }

            try
            {
                // check if media item is not already in the database
                var existingItem = await _database.Table<MediaItem>().Where(i => i.FilePath == item.FilePath).FirstOrDefaultAsync();
                if (existingItem != null)
                {
                    _logger.LogWarning("Media item with path '{Path}' already exists in the database. Skipping.", item.FilePath);
                    return;
                }

                // add the media item into the database
                await _database.InsertAsync(item);
                _logger.LogInformation("Successfully added '{FileName}' to the database.", item.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add media item '{FileName}' to database.", item.FileName);
            }
        }

        public async Task<List<MediaItem>> GetAllItemsForFolderAsync(string folderPath, int pageNumber, int pageSize, string? filterText = null)
        {
            await InitAsync();
            if (_database is null)
            {
                return new List<MediaItem>();
            }

            _logger.LogInformation("Fetching page {PageNumber} for folder '{Path}'", pageNumber, folderPath);

            var query = _database.Table<MediaItem>().Where(i => i.FilePath.StartsWith(folderPath));

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                var searchTerms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var term in searchTerms)
                {
                    query = query.Where(i => i.Tags.Contains(term));
                }
            }

            return await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task DeleteMediaItemAsync(MediaItem item)
        {
            await InitAsync();
            if (_database is null)
            {
                return;
            }

            try
            {
                await _database.DeleteAsync(item);
                _logger.LogInformation("Successfully deleted database record for '{FileName}'", item.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete database record for '{FileName}'", item.FileName);
            }
        }

        // synchronization service - deleting ghost records
        public async Task DeleteItemsByPathsAsync(IEnumerable<string> paths)
        {
            await InitAsync();
            if (_database is null)
            {
                return;
            }

            await _database.RunInTransactionAsync(tran =>
            {
                foreach (var path in paths)
                {
                    tran.Execute("DELETE FROM MediaItems WHERE FilePath = ?", path);
                    _logger.LogInformation("Deleted ghost record from database for path '{Path}'", path);
                }
            });
        }

        public async Task UpdateMediaItemAsync(MediaItem item)
        {
            await InitAsync();
            if (_database is null)
            {
                return;
            }

            try
            {
                await _database.UpdateAsync(item);
                _logger.LogInformation("Successfully updated database record for '{FileName}'", item.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update the database record for '{FileName}'", item.FileName);
            }
        }

        public async Task<int> GetItemCountForFolderAsync(string folderPath, string? filterText = null)
        {
            await InitAsync();
            if (_database is null)
            {
                return 0;
            }

            var query = _database.Table<MediaItem>().Where(i => i.FilePath.StartsWith(folderPath));

            if (!string.IsNullOrWhiteSpace(filterText))
            {
                var searchTerms = filterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var term in searchTerms)
                {
                    query = query.Where(i => i.Tags.Contains(term));
                }
            }

            return await query.CountAsync();
        }

        public async Task<List<string>> GetFilePathsForFolderAsync(string folderPath)
        {
            await InitAsync();
            if (_database is null)
            {
                return new List<string>();
            }

            return await _database.QueryScalarsAsync<string>("SELECT FilePath FROM MediaItems WHERE FilePath LIKE ?", folderPath + "%");
        }
    }
}
