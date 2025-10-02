using System.Windows;
using System.Windows.Controls;
using static stepstones.Resources.AppConstants;

namespace stepstones.Views
{
    public partial class EnlargeImageView : UserControl
    {
        public int MinSize => MinimumDisplaySize;

        public EnlargeImageView()
        {
            InitializeComponent();
            this.Unloaded += (s, e) => ImageViewer.Source = null;
        }
    }
}
