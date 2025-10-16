namespace stepstones.Services.Infrastructure
{
    public interface ISettingsService
    {
        Task<string?> LoadMediaFolderPathAsync();
        void SaveMediaFolderPath(string path);
    }
}
