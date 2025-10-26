using stepstones.Models;

namespace stepstones.Services.Interaction
{
    public interface IDialogService
    {
        object? ActiveDialogViewModel { get; }

        void ShowDialog(object viewModel);
        Task<EditTagsResult> ShowEditTagsDialogAsync(string? currentTags);
        void CloseDialog(string? result);
    }
}
