using System.Windows;
using System.Windows.Input;
using stepstones.ViewModels;

namespace stepstones
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // scroll to the top on page change
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(viewModel.CurrentPage))
                {
                    MainScrollViewer.ScrollToTop();
                }
            };
        }

        // to allow user to make filter textbox "inactive" when clicked anywhere else
        private void MainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MainGrid.Focusable = true;
            MainGrid.Focus();
            MainGrid.Focusable = false;
        }
    }
}