using stepstones.Models;

namespace stepstones.Services.Interaction
{
    public interface IDialogService
    {
        object? ActiveDialogViewModel { get; }

        void ShowDialog(object viewModel);
        void ShowTranscodingDialog(CancellationTokenSource cancellationTokenSource);
        Task<EditTagsResult> ShowEditTagsDialogAsync(string? currentTags);
        void CloseDialog(string? result);
    }
}
