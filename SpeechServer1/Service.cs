using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Speech.Synthesis;
using System.Threading;
using System.Configuration;

namespace SpeechServer1
{
    public class Service : ServiceBase
    {
        private readonly EventLog _logger;
        private readonly Server _server;

        public Service()
        {
#if !DEBUG
            AutoLog = false;
            var srcName = ConfigurationManager.AppSettings["eventSourceName"];
            var logName = ConfigurationManager.AppSettings["eventLogName"];

            if (!EventLog.SourceExists(srcName))
            {
                EventLog.CreateEventSource(srcName, logName);
            }

            _logger = new EventLog {Source = srcName};
            _server = new Server();
#endif
        }

        protected override void OnStart(string[] args)
        {
            var th = new Thread(_server.Listen);
            th.Start();
            _logger?.WriteEntry("Service started.");
        }

        protected override void OnStop()
        {
            _server?.Stop();
            _logger?.WriteEntry("Service Stopped.");
        }
    }
}