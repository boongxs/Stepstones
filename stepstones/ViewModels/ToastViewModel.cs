using CommunityToolkit.Mvvm.ComponentModel;
using stepstones.Enums;

namespace stepstones.ViewModels
{
    public partial class ToastViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _message;

        [ObservableProperty]
        private ToastNotificationType _type;

        public ToastViewModel(string message, ToastNotificationType type)
        {
            _message = message;
            _type = type;
        }
    }
}