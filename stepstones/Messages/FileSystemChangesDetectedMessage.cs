using System.Collections.Generic;

namespace stepstones.Messages
{
    public class FileSystemChangesDetectedMessage
    {
        public List<string> NewFilePaths { get; }
        public Dictionary<string, string> RenamedFilePaths { get; }
        public List<string> DeletedFilePaths { get; }

        public FileSystemChangesDetectedMessage(
            List<string> newFilePaths,
            Dictionary<string, string> renamedFilePaths,
            List<string> deletedFilePaths)
        {
            NewFilePaths = newFilePaths;
            RenamedFilePaths = renamedFilePaths;
            DeletedFilePaths = deletedFilePaths;
        }
    }
}
