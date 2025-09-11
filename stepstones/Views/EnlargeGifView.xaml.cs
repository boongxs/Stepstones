using System.Windows.Controls;

namespace stepstones.Views
{
    public partial class EnlargeGifView : UserControl
    {
        private const int MinimumDisplaySize = 400;
        public int MinSize => MinimumDisplaySize;

        public EnlargeGifView()
        {
            InitializeComponent();
        }
    }
}
