using CommunityToolkit.Mvvm.ComponentModel;

namespace stepstones.ViewModels
{
    public partial class EditTagsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _tagsText;

        public EditTagsViewModel(string? currentTags)
        {
            // pre-populate the text box with existing tags
            _tagsText = currentTags;
        }
    }
}
