using System.ServiceProcess;

namespace SpeechServer1
{
    public class Service : ServiceBase
    {
        private Server _server;
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            _server = new Server();
            _server.Listen();
        }

        protected override void OnStop()
        {
            _server.Stop();
        }
    }
}