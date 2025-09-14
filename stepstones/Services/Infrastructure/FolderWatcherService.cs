using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using stepstones.Messages;
using stepstones.Services.Data;
using System.Diagnostics;

namespace stepstones.Services.Infrastructure
{
    public class FolderWatcherService : IFolderWatcherService, IDisposable
    {
        private readonly ILogger<FolderWatcherService> _logger;
        private readonly IMessenger _messenger;
        private readonly IDatabaseService _databaseService;
        private FileSystemWatcher? _watcher;
        private Timer? _debounceTimer;

        private readonly ConcurrentBag<string> _createdFiles = new ConcurrentBag<string>();
        private readonly ConcurrentBag<string> _deletedFiles = new ConcurrentBag<string>();
        private readonly ConcurrentBag<RenamedEventArgs> _renamedEvents = new ConcurrentBag<RenamedEventArgs>();

        public FolderWatcherService(ILogger<FolderWatcherService> logger, 
                                    IMessenger messenger,
                                    IDatabaseService databaseService)
        {
            _logger = logger;
            _messenger = messenger;
            _databaseService = databaseService;
        }

        public void StartWatching(string folderPath)
        {
            StopWatching();

            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Cannot watch folder '{Path}' because it does not exist.", folderPath);
                return;
            }

            _watcher = new FileSystemWatcher(folderPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;

            _debounceTimer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Started watching '{Path}'", folderPath);
        }

        public void StopWatching()
        {
            if ( _watcher != null )
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileCreated;
                _watcher.Deleted -= OnFileDeleted;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Dispose();
                _watcher = null;
                _logger.LogInformation("Stopped watching folder.");
            }

            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File created: {Path}", e.FullPath);
            _createdFiles.Add(e.FullPath);
            ResetTimer();
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            _logger.LogDebug("File deleted: {Path}", e.FullPath);
            _deletedFiles.Add(e.FullPath);
            ResetTimer();
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            _logger.LogDebug("File renamed: from {OldPath} to {NewPath}", e.OldFullPath, e.FullPath);
            _renamedEvents.Add(e);
            ResetTimer();
        }

        private void ResetTimer()
        {
            _debounceTimer?.Change(2000, Timeout.Infinite);
        }

        private async void OnTimerElapsed(object? state)
        {
            _logger.LogInformation("Debounce timer elapsed. Processing file system changes.");

            var created = _createdFiles.ToList();
            var deleted = _deletedFiles.ToList();
            var renamed = _renamedEvents.ToList();

            // clear the bags for the next batch of events
            while (_createdFiles.TryTake(out _)) { }
            while (_deletedFiles.TryTake(out _)) { }
            while (_renamedEvents.TryTake(out _)) { }

            var finalNewFiles = new List<string>();
            var finalRenamedFiles = new Dictionary<string, string>();
            var finalDeletedFiles = new List<string>();

            // process renames first to update paths
            foreach (var e in renamed)
            {
                // check if this rename was part of quick "create-then-rename" operation
                if (created.Contains(e.OldFullPath))
                {
                    finalNewFiles.Add(e.FullPath);
                    created.Remove(e.OldFullPath);
                }
                else
                {
                    finalRenamedFiles[e.OldFullPath] = e.FullPath;
                }
            }

            // process created files
            finalNewFiles.AddRange(created);

            // process deleted files
            finalDeletedFiles.AddRange(deleted);

            if (finalNewFiles.Any() ||  finalRenamedFiles.Any() || finalDeletedFiles.Any())
            {
                _logger.LogInformation("Sending file system changes: {NewCount} new, {RenamedCount} renamed, {DeletedCount} deleted.", finalNewFiles.Count, finalRenamedFiles.Count, finalDeletedFiles.Count);

                _messenger.Send(new FileSystemChangesDetectedMessage(finalNewFiles, finalRenamedFiles, finalDeletedFiles));
            }
            else
            {
                _logger.LogInformation("No effective file system changes to report.");
            }
        }

        public void Dispose()
        {
            StopWatching();
        }
    }
}
