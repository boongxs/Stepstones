using System.Windows;
using System.Windows.Controls;

namespace stepstones.Views
{
    public partial class EditTagsView : UserControl
    {
        public EditTagsView()
        {
            InitializeComponent();
            this.Loaded += EditTagsView_Loaded;
        }

        private void EditTagsView_Loaded(object sender, RoutedEventArgs e)
        {
            TagsTextBox.Focus();
            TagsTextBox.Select(TagsTextBox.Text.Length, 0);
        }
    }
}
