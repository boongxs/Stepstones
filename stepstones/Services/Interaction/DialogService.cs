using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using stepstones.Models;
using stepstones.ViewModels;
using stepstones.Resources;

namespace stepstones.Services.Interaction
{
    public partial class DialogService : ObservableObject, IDialogService
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverlayVisible))]
        private object? _activeDialogViewModel;

        public bool IsOverlayVisible => ActiveDialogViewModel != null;

        private TaskCompletionSource<EditTagsResult>? _editTagsCompletionSource;

        private CancellationTokenSource? _transcodingCts;

        public void ShowDialog(object viewModel)
        {
            ActiveDialogViewModel = viewModel;
        }

        public void ShowTranscodingDialog(CancellationTokenSource cancellationTokenSource)
        {
            _transcodingCts = cancellationTokenSource;
            ActiveDialogViewModel = new TranscodingProgressViewModel();
        }

        public Task<EditTagsResult> ShowEditTagsDialogAsync(string? currentTags)
        {
            _editTagsCompletionSource = new TaskCompletionSource<EditTagsResult>();
            ActiveDialogViewModel = new EditTagsViewModel(currentTags);

            return _editTagsCompletionSource.Task;
        }

        [RelayCommand]
        public void CloseDialog(string? result)
        {
            if (ActiveDialogViewModel is EditTagsViewModel editTagsVM)
            {
                var wasSaved = result == AppConstants.SaveCommandParameter;
                var dialogResult = new EditTagsResult { WasSaved = wasSaved, NewTags = editTagsVM.TagsText };
                _editTagsCompletionSource?.SetResult(dialogResult);
            }
            else if (ActiveDialogViewModel is TranscodingProgressViewModel)
            {
                _transcodingCts?.Cancel();
            }

            _transcodingCts = null;
            ActiveDialogViewModel = null;
        }
    }
}
