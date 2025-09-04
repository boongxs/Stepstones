using System.Windows;
using System.Windows.Controls;

namespace stepstones.Views
{
    public partial class EnlargeImageView : UserControl
    {
        public EnlargeImageView()
        {
            InitializeComponent();
            this.Unloaded += (s, e) => ImageViewer.Source = null;
        }
    }
}
