using CommunityToolkit.Mvvm.ComponentModel;

namespace stepstones.ViewModels
{
    public partial class EditTagsViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? _tagsText;

        public string? Result { get; private set; }

        public EditTagsViewModel(string? currentTags)
        {
            _tagsText = currentTags;
        }

        public void Save()
        {
            Result = TagsText;
        }
    }
}
