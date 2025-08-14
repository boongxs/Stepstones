using stepstones.Models;
using System.Threading.Tasks;

namespace stepstones.Services.Core
{
    public interface IFileTypeIdentifierService
    {
        Task<MediaType> IdentifyAsync(string filePath);
    }
}
