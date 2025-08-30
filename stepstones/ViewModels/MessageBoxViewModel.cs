using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace stepstones.ViewModels
{
    public enum MessageBoxType
    {
        Ok,
        YesNo
    }

    public partial class MessageBoxViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _title;

        [ObservableProperty]
        private string _message;

        public bool? Result { get; private set; }

        public bool IsOkVisible { get; }
        public bool IsYesNoVisible { get; }

        private readonly Window _view;

        public MessageBoxViewModel(string title, string message, MessageBoxType type, Window view)
        {
            _title = title;
            _message = message;
            _view = view;

            IsOkVisible = type == MessageBoxType.Ok;
            IsYesNoVisible = type == MessageBoxType.YesNo;
        }

        [RelayCommand]
        private void Yes()
        {
            Result = true;
            _view.DialogResult = true;
            _view.Close();
        }

        [RelayCommand]
        private void No()
        {
            Result = false;
            _view.DialogResult = false;
            _view.Close();
        }

        [RelayCommand]
        private void Ok()
        {
            Result = true;
            _view.DialogResult = true;
            _view.Close();
        }
    }
}
