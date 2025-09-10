namespace stepstones.Services.Data
{
    public interface IDataMigrationService
    {
        void RunMigration(string folderPath, Action? onCompleted = null);
    }
}
