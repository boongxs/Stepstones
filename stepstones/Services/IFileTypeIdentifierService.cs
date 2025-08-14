using System.Threading.Tasks;
using stepstones.Models;

namespace stepstones.Services
{
    public interface IFileTypeIdentifierService
    {
        Task<MediaType> IdentifyAsync(string filePath);
    }
}
