using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace SpeechServer1
{
    class ReqBody
    {
        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public String Phrase { get; set; }
        
        [DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Rate { get; set; }

        [DefaultValue(100)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Volume { get; set; }

        public override string ToString()
        {
            return $"Phrase: {Phrase}, Rate: {Rate}, Volume: {Volume}";
        }
    }
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

            _listener = new HttpListener {IgnoreWriteExceptions = true};

            String[] urls = {$"http://+:{_port}/"};
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
            _listener.Stop();
        }

        async Task HandleIncoming()
        {
            for (;;)
            {
                var ctx = await _listener.GetContextAsync();
                var req = ctx.Request;
                var res = ctx.Response;
                Console.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss.ff}] {req.HttpMethod} {req.Url.AbsolutePath}");

                if (req.HttpMethod.Equals("GET"))
                {
                    res.StatusCode = 200;
                    res.ContentType = "text/plain";
                    var b = Encoding.UTF8.GetBytes("ok");
                    await res.OutputStream.WriteAsync(b, 0, b.Length);
                    res.Close();
                }
                if (!req.HttpMethod.Equals("POST") || req.Headers.Get("range") != null)
                {
                    continue;
                }


                if (req.Url.AbsolutePath == "/say")
                {
                    var reader = new JsonTextReader(new StreamReader(req.InputStream));
                    var config = new JsonSerializer().Deserialize<ReqBody>(reader);
                    Console.WriteLine(config);

                    var tempStream = new MemoryStream();
                    _engine.Volume = config.Volume;
                    _engine.Rate = config.Rate;
                    _engine.SetOutputToWaveStream(tempStream);
                    _engine.Speak(config.Phrase);

                    tempStream.Seek(0, SeekOrigin.Begin);
                    var bytes = tempStream.ToArray();
                    res.ContentLength64 = bytes.Length;
                    res.ContentType = "audio/wav";
                    res.SendChunked = true;
                    res.OutputStream.Write(bytes, 0, bytes.Length);
                    res.Close();
                    continue;
                }

                res.StatusCode = 200;
                res.Close();
            }
        }

        public void Stop()
        {
            _listener.Stop();
        }
    }
}