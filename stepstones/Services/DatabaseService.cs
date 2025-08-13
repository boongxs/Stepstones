using Microsoft.Extensions.Logging;
using SQLite;
using System.IO;
using stepstones.Models;

namespace stepstones.Services
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
                _logger.LogError("Database is not initialize, cannot add media item.");
                return;
            }

            try
            {
                var existingItem = await _database.Table<MediaItem>().Where(i => i.FilePath == item.FilePath).FirstOrDefaultAsync();
                if (existingItem != null)
                {
                    _logger.LogWarning("Media item with path '{Path}' already exists in the database. Skipping.", item.FilePath);
                    return;
                }

                await _database.InsertAsync(item);
                _logger.LogInformation("Successfully added '{FileName}' to the database.", item.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add media item '{FileName}' to database.", item.FileName);
            }
        }

        public async Task<List<string>> GetAllFilePathsAsync()
        {
            await InitAsync();
            if (_database is null)
            {
                return new List<string>();
            }

            var allItems = await _database.Table<MediaItem>().ToListAsync();
            return allItems.Select(i => i.FilePath).ToList();
        }

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

        public async Task<List<MediaItem>> GetAllItemsForFolderAsync(string folderPath)
        {
            await InitAsync();
            if (_database is null)
            {
                return new List<MediaItem>();
            }

            _logger.LogInformation("Fetching media items for folder '{Path}'", folderPath);

            return await _database.Table<MediaItem>()
                .Where(i => i.FilePath.StartsWith(folderPath))
                .ToListAsync();
        }
    }
}
