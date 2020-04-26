using System;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace SpeechServer1
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
#if DEBUG || true
            Console.OutputEncoding = Encoding.UTF8;
            var server = new Server();
            var th = new Thread(server.Listen);
            th.Start();
#else
            ServiceBase.Run(new Service());
#endif
        }
    }
}