using System.Windows;
using System.Windows.Input;
using stepstones.ViewModels;
using static stepstones.Resources.AppConstants;

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

        // itemscontrol responsive grid layout depending on window dimensions
        private void MediaItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            double availableWidth = e.NewSize.Width;

            if (availableWidth <= 0 || viewModel == null)
            {
                return;
            }

            double desiredThumbnailWidth = DesiredThumbnailWidth;
            double horizontalItemMargin = HorizontalItemMargin;

            int newColumns = (int)Math.Max(1, Math.Floor(availableWidth / (desiredThumbnailWidth + horizontalItemMargin)));

            if (viewModel.GridColumns != newColumns)
            {
                viewModel.GridColumns = newColumns;
            }
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