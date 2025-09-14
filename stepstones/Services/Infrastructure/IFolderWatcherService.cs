namespace stepstones.Services.Infrastructure
{
    public interface IFolderWatcherService
    {
        void StartWatching(string folderPath);
        void StopWatching();
    }
}
