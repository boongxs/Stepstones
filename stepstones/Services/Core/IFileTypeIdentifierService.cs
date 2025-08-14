using System.Threading.Tasks;
using stepstones.Models;

namespace stepstones.Services.Core
{
    public interface IFileTypeIdentifierService
    {
        Task<MediaType> IdentifyAsync(string filePath);
    }
}
