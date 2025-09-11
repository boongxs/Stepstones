using System.Windows;
using System.Windows.Controls;

namespace stepstones.Views
{
    public partial class EnlargeImageView : UserControl
    {
        private const int MinimumDisplaySize = 400;
        public int MinSize => MinimumDisplaySize;

        public EnlargeImageView()
        {
            InitializeComponent();
            this.Unloaded += (s, e) => ImageViewer.Source = null;
        }
    }
}
