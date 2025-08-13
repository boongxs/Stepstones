namespace stepstones.Services
{
    public interface IFileService
    {
        Task CopyFilesAsync(IEnumerable<string> sourceFilePaths, string destinationFolderPath);
    }
}
