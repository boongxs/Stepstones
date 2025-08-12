namespace stepstones.Services
{
    public interface ISettingsService
    {
        string? LoadMediaFolderPath();
        void SaveMediaFolderPath(string path);
    }
}
