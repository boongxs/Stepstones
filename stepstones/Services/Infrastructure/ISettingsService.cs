namespace stepstones.Services.Infrastructure
{
    public interface ISettingsService
    {
        string? LoadMediaFolderPath();
        void SaveMediaFolderPath(string path);
    }
}
