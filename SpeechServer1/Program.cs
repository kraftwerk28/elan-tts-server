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
        private readonly HttpListener _listener;
        private readonly SpeechSynthesizer _engine;
        private readonly int _port = 8080;

        public Server()
        {
            if (Int32.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
            {
                _port = port;
            }

            _listener = new HttpListener();
            _listener.IgnoreWriteExceptions = true;
            String[] urls =
            {
                $"http://+:{_port}/",
                //$"http://127.0.0.1:{_port}/"
            };
            foreach (var url in urls)
            {
                _listener.Prefixes.Add(url);
            }

            _engine = new SpeechSynthesizer();
            _engine.SelectVoice("ELAN TTS Russian (Nicolai 16Khz)");
        }

        public void Listen()
        {
            Console.WriteLine("Serving port :{0}", _port);
            _listener.Start();

            var listenTask = HandleIncoming();
            listenTask.GetAwaiter().GetResult();

            _listener.Close();
        }

        async Task HandleIncoming()
        {
            while (true)
            {
                var ctx = await _listener.GetContextAsync();
                var req = ctx.Request;
                var res = ctx.Response;
                var q = HttpUtility.ParseQueryString(req.Url.Query);
                if (req.Headers.Get("range") != null)
                {
                    continue;
                }
                
                if (req.Url.AbsolutePath == "/say")
                {

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
                    _engine.Volume = volume;
                    _engine.Rate = rate;
                    _engine.SetOutputToWaveStream(tempStream);
                    _engine.Speak(phrase);

                    tempStream.Seek(0, SeekOrigin.Begin);
                    var bytes = tempStream.ToArray();
                    res.ContentLength64 = bytes.Length;
                    res.ContentType = "audio/wav";
                    res.SendChunked = true;
                    res.KeepAlive = false;
                    res.OutputStream.Write(bytes, 0, bytes.Length);
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