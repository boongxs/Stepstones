using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace stepstones.Services.Core
{
    public static class FileNameGenerator
    {
        public static string GenerateUniqueFileName(string sourceFilePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(sourceFilePath);

            var hashBytes = md5.ComputeHash(stream);
            var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            var extension = Path.GetExtension(sourceFilePath);

            return $"{hashString}{extension}";
        }
    }
}
