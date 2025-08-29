using System.Windows;
using stepstones.ViewModels;
using stepstones.Views;

namespace stepstones.Services.Interaction
{
    public class MessageBoxService : IMessageBoxService
    {
        public void Show(string message)
        {
            ShowDialog("Stepstones", message, false);
        }

        public bool ShowConfirmation(string title, string message)
        {
            return ShowDialog(title, message, true);
        }

        private bool ShowDialog(string title, string message, bool isConfirmation)
        {
            var viewModel = new MessageBoxViewModel(title, message, isConfirmation);
            var messageBoxView = new MessageBoxView
            {
                DataContext = viewModel,
                Owner = Application.Current.MainWindow
            };

            messageBoxView.ShowDialog();

            return viewModel.DialogResult ?? false;
        }
    }
}
