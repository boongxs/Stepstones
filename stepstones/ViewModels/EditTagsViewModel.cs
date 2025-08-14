using CommunityToolkit.Mvvm.ComponentModel;

namespace stepstones.ViewModels
{
    public partial class EditTagsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _tagsText;

        public EditTagsViewModel(string? currentTags)
        {
            _tagsText = currentTags;
        }
    }
}
