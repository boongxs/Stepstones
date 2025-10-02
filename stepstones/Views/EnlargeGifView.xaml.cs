using System.Windows.Controls;
using static stepstones.Resources.AppConstants;

namespace stepstones.Views
{
    public partial class EnlargeGifView : UserControl
    {
        public int MinSize => MinimumDisplaySize;

        public EnlargeGifView()
        {
            InitializeComponent();
        }
    }
}
