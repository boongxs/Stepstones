using stepstones.Models;

namespace stepstones.Services.Data
{
    public interface IDataMigrationService
    {
        void RunMigration(string folderPath, Action<MediaItem> onItemRepaired);
    }
}
