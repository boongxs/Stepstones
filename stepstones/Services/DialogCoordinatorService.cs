using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using stepstones.Messages;
using stepstones.Models;
using stepstones.ViewModels;

namespace stepstones.Services
{
    public class DialogCoordinatorService : IDialogCoordinatorService
    {
        private readonly IMessenger _messenger;
        private TaskCompletionSource<EditTagsResult>? _tcs;

        public DialogCoordinatorService(IMessenger messenger)
        {
            _messenger = messenger;
            _messenger.Register<DialogClosedMessage>(this, OnDialogClosed);
        }

        public Task<EditTagsResult> ShowEditTagsDialogAsync(string? initialTags)
        {
            _tcs = new TaskCompletionSource<EditTagsResult>();

            var viewModel = new EditTagsViewModel(initialTags);
            _messenger.Send(new ShowDialogMessage(viewModel));

            return _tcs.Task;
        }

        private void OnDialogClosed(object recipient, DialogClosedMessage message)
        {
            if (_tcs != null && !_tcs.Task.IsCompleted)
            {
                if (message.Result is EditTagsResult result)
                {
                    _tcs.SetResult(result);
                }
                else
                {
                    _tcs.SetResult(new EditTagsResult { WasSaved = true });
                }
            }
        }
    }
}
