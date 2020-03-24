using System;
using System.ServiceProcess;
using System.Text;

namespace SpeechServer1
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var server = new Server();
            server.Listen();
//            ServiceBase.Run(new Service());
        }
    }
}