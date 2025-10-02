using Ookii.Dialogs.Wpf;
using static stepstones.Resources.AppConstants;

namespace stepstones.Services.Interaction
{
    public class FileDialogService : IFileDialogService
    {
        public IEnumerable<string>? ShowDialog()
        {
            var dialog = new VistaOpenFileDialog
            {
                Title = SelectFilesDialogTitle,
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
