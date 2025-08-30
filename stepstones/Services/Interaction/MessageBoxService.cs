using System.Windows;
using stepstones.ViewModels;
using stepstones.Views;

namespace stepstones.Services.Interaction
{
    public class MessageBoxService : IMessageBoxService
    {
        public void Show(string title, string message)
        {
            ShowDialog(title, message, MessageBoxType.Ok);
        }

        public bool ShowConfirmation(string title, string message)
        {
            return ShowDialog(title, message, MessageBoxType.YesNo);
        }

        private bool ShowDialog(string title, string message, MessageBoxType type)
        {
            var messageBoxView = new MessageBoxView
            {
                Owner = Application.Current.MainWindow
            };

            var viewModel = new MessageBoxViewModel(title, message, type, messageBoxView);

            messageBoxView.DataContext = viewModel;
            messageBoxView.ShowDialog();

            return viewModel.Result ?? false;
        }
    }
}
