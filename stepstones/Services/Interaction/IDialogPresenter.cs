using stepstones.Models;

namespace stepstones.Services.Interaction
{
    public interface IDialogPresenter
    {
        Task<EditTagsResult> ShowEditTagsDialogAsync(string? currentTags);
    }
}
