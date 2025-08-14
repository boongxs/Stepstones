using System.Threading.Tasks;
using stepstones.Models;

namespace stepstones.Services
{
    public interface IDialogCoordinatorService
    {
        Task<EditTagsResult> ShowEditTagsDialogAsync(string? initialTags);
    }
}
