using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace filewatch
{
    public partial class Service : ServiceBase
    {
        private string data;
        private string filename;
        private EventLog _eventlog;
        private volatile bool _shouldStop;
        private Thread _workerThread;
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
            _eventlog.WriteEntry("FileWatcher: Configuring...", EventLogEntryType.Information);
            if (args.Length > 0)
            {
                _eventlog.WriteEntry($"FileWatcher: Setting {args[0]} as path", EventLogEntryType.Information);
                filename = args[0];
            }
            else
            {
                _eventlog.WriteEntry("FileWatcher: Invalid args, cannot watch file.", EventLogEntryType.Error);
                Stop();
                return;

            }
            _eventlog.WriteEntry("FileWatcher: Testing file...", EventLogEntryType.Information);
            try
            {
                data = File.ReadAllText(filename);
            }
            catch (Exception e)
            {
                _eventlog.WriteEntry($"FileWatcher: Fatal Error {e.Message}\n{e.StackTrace}", EventLogEntryType.Error);
                Stop();
                return;
            }

            _eventlog.WriteEntry("FileWatcher: Configuring Done", EventLogEntryType.Information);
            _shouldStop = false;
            _workerThread = new Thread(DoWork) { IsBackground = true };
            _workerThread.Start();
            _eventlog.WriteEntry("FileWatcher: Started Background Service", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _eventlog.WriteEntry("FileWatcher: Stopping Service", EventLogEntryType.Information);
            _shouldStop = true;

            if (_workerThread != null && _workerThread.IsAlive) 
            {
                _workerThread.Join();
            }
            _eventlog.WriteEntry("FileWatcher: Service Stopped Cleanly", EventLogEntryType.Information);
        }

        private void DoWork() 
        {
            _eventlog.WriteEntry("FileWatcher: Background worker running", EventLogEntryType.Information);

            while (!_shouldStop) 
            {
                try
                {
                    string newdata = File.ReadAllText(filename);
                    if (newdata != data)
                    {
                        _eventlog.WriteEntry($"FileWatcher: Change Detected.\nOriginal: {data}\nNew: {newdata}");
                        data = newdata;
                        newdata = "";
                    }
                    Thread.Sleep(30000);
                }
                catch (Exception e)
                {
                    _eventlog.WriteEntry($"FileWatcher: Error {e.Message}\n{e.StackTrace}", EventLogEntryType.Error);
                    Stop();
                    return;
                }
            }
        }
    }
}
