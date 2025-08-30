using System.Windows;
using System.Windows.Input;

namespace stepstones.Views
{ 
    public partial class MessageBoxView : Window
    {
        public MessageBoxView()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
