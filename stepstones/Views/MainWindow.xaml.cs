using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using stepstones.ViewModels;

namespace stepstones
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }

        private void MediaItemsControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var viewModel = DataContext as MainViewModel;
            double availableWidth = e.NewSize.Width;

            if (availableWidth <= 0 || viewModel == null)
            {
                return;
            }

            double desiredThumbnailWidth = 270;
            double horizontalItemMargin = 10;

            int newColumns = (int)Math.Max(1, Math.Floor(availableWidth / (desiredThumbnailWidth + horizontalItemMargin)));

            if (viewModel.GridColumns != newColumns)
            {
                viewModel.GridColumns = newColumns;
            }
        }
    }
}