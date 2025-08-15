using Microsoft.Extensions.Logging;
using System.Collections.Specialized;
using System.IO;
using System.Windows;

namespace stepstones.Services.Infrastructure
{
    public class ClipboardService : IClipboardService
    {
        private readonly ILogger<ClipboardService> _logger;

        public ClipboardService(ILogger<ClipboardService> logger)
        {
            _logger = logger;
        }

        public void CopyFileToClipboard(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    _logger.LogWarning("Copy to clipboard skipped: '{Path}' is invalid or does not exist.", filePath);
                    return;
                }

                var fileCollection = new StringCollection { filePath };
                var dataObject = new DataObject();
                dataObject.SetFileDropList(fileCollection);

                Clipboard.SetDataObject(dataObject, true);
                _logger.LogInformation("Successfully copied '{Path}' to clipboard.", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy '{Path}' to clipboard.", filePath);
            }
        }
    }
}
