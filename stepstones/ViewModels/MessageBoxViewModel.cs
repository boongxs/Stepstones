using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace stepstones.ViewModels
{
    public partial class MessageBoxViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private bool _isConfirmation;

        public bool? DialogResult { get; private set; }

        public MessageBoxViewModel(string title, string message, bool isConfirmation)
        {
            _title = title;
            _message = message;
            _isConfirmation = isConfirmation;
        }

        [RelayCommand]
        private void Yes(Window window)
        {
            DialogResult = true;
            window.Close();
        }

        [RelayCommand]
        private void No(Window window)
        {
            DialogResult = false;
            window.Close();
        }

        [RelayCommand]
        private void Ok(Window window)
        {
            DialogResult = true;
            window.Close();
        }
    }
}
