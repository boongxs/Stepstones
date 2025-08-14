using Ookii.Dialogs.Wpf;

namespace stepstones.Services.Interaction
{
    public class FileDialogService : IFileDialogService
    {
        public IEnumerable<string>? ShowDialog()
        {
            var dialog = new VistaOpenFileDialog
            {
                Title = "Select file(s) to upload.",
                Multiselect = true
            };

            if (dialog.ShowDialog().GetValueOrDefault())
            {
                return dialog.FileNames;
            }

            return null;
        }
    }
}
