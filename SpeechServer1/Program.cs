using System;
using System.Speech.Synthesis;
using System.Text;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace SpeechServer1
{
    class Server
    {
        readonly HttpListener _listener;
        readonly SpeechSynthesizer _engine;
        private readonly int _port = 8080;

        public Server()
        {
            if (Int32.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
            {
                _port = port;
            }
            
            _listener = new HttpListener();
            var url = $"http://127.0.0.1:{_port}/";
            _listener.Prefixes.Add(url);
            _engine = new SpeechSynthesizer();
            _engine.SelectVoice("ELAN TTS Russian (Nicolai 16Khz)");
        }
        public void Listen()
        {
            _listener.Start();

            Task listenTask = HandleIncoming();
            listenTask.GetAwaiter().GetResult();

            _listener.Close();
        }

        async Task HandleIncoming()
        {
            bool runServer = true;

            while (runServer)
            {
                var ctx = await _listener.GetContextAsync();
                var req = ctx.Request;
                var res = ctx.Response;
                var q = HttpUtility.ParseQueryString(req.Url.Query);

                if (req.Url.AbsolutePath == "/say")
                {
                    res.ContentType = "audio/wav";

                    var phrase = Uri.UnescapeDataString(q.Get("q"));
                    var qRate = q.Get("rate");
                    var qVolume = q.Get("volume");

                    if (Int32.TryParse(qRate, out var rate))
                    {
                        if (rate < 0) rate = 0;
                        if (rate > 10) rate = 10;                        
                    }
                    if (Int32.TryParse(qVolume, out var volume))
                    {
                        if (volume < 1) volume = 1;
                        if (volume > 100) volume = 100;                  
                    }

                    var tempStream = new MemoryStream();
                    _engine.SetOutputToWaveStream(tempStream);
                    _engine.Volume = volume;
                    _engine.Rate = rate;
                    _engine.Speak(phrase);

                    res.ContentLength64 = tempStream.Length;
                    var bytes = tempStream.ToArray();
                    await res.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    res.Close();
                    continue;
                }

                res.StatusCode = 200;
                res.Close();
            }
        }
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var server = new Server();
            server.Listen();
        }
    }
}