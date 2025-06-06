using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace filewatch
{
    public partial class Service : ServiceBase
    {
        private string filename;
        private EventLog _eventlog;
        private FileSystemWatcher _watcher;
        private const string EventSourceName = "FileWatcher";
        private const string EventLogName = "Application";

        public Service()
        {
            InitializeComponent();
            this.CanStop = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;

            _eventlog = new EventLog();

            if (!EventLog.SourceExists(EventSourceName))
            {
                EventLog.CreateEventSource(EventSourceName, EventLogName);
            }

            _eventlog.Source = EventSourceName;
            _eventlog.Log = EventLogName;
            _eventlog.WriteEntry("FileWatcher: Constructor called", EventLogEntryType.Information);
        }

        protected override void OnStart(string[] args)
        {
            _eventlog.WriteEntry("FileWatcher: Starting service", EventLogEntryType.Information);
            _eventlog.WriteEntry("FileWatcher: Reading configuration...", EventLogEntryType.Information);

            filename = ConfigurationManager.AppSettings["WatchFilePath"];

            if (string.IsNullOrEmpty(filename))
            {
                _eventlog.WriteEntry("FileWatcher: No file path configured in App.config, stopping service.", EventLogEntryType.Error);
                Stop();
                return;
            }

            if (!File.Exists(filename))
            {
                _eventlog.WriteEntry($"FileWatcher: File does not exist: {filename}, stopping service.", EventLogEntryType.Error);
                Stop();
                return;
            }

            _eventlog.WriteEntry($"FileWatcher: Watching file: {filename}", EventLogEntryType.Information);

            // Setup FileSystemWatcher to monitor the directory of the file
            string directory = Path.GetDirectoryName(filename);
            string file = Path.GetFileName(filename);

            _watcher = new FileSystemWatcher(directory, file)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };

            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnDeleted;
            _watcher.Error += OnError;

            _watcher.EnableRaisingEvents = true;

            _eventlog.WriteEntry("FileWatcher: FileSystemWatcher started", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _eventlog.WriteEntry("FileWatcher: Stopping service", EventLogEntryType.Information);

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }

            _eventlog.WriteEntry("FileWatcher: Service stopped cleanly", EventLogEntryType.Information);
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Retry logic in case file is locked
                int retries = 3;
                while (retries > 0)
                {
                    try
                    {
                        string newData = File.ReadAllText(filename);
                        _eventlog.WriteEntry($"FileWatcher: File changed at {DateTime.Now}. New content length: {newData.Length} chars.", EventLogEntryType.Information);
                        break;
                    }
                    catch (IOException)
                    {
                        retries--;
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                _eventlog.WriteEntry($"FileWatcher: Error reading changed file: {ex.Message}", EventLogEntryType.Error);
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            _eventlog.WriteEntry($"FileWatcher: File renamed from {e.OldFullPath} to {e.FullPath}", EventLogEntryType.Warning);
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            _eventlog.WriteEntry("FileWatcher: File deleted, stopping service.", EventLogEntryType.Error);
            Stop();
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _eventlog.WriteEntry($"FileWatcher: FileSystemWatcher error: {e.GetException().Message}", EventLogEntryType.Error);
        }

    }
}
