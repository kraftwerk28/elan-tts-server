using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SpeechServer1
{
    internal class ReqBody
    {
        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Phrase { get; set; }

        [DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Rate { get; set; }

        [DefaultValue(100)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public int Volume { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string Voice { get; set; }

        public override string ToString()
        {
            return $"Phrase: {Phrase}, Rate: {Rate}, Volume: {Volume}";
        }
    }

    public class Server
    {
        private readonly Dictionary<string, string> _voiceNames = new Dictionary<string, string>
        {
            {"maxim", "IVONA 2 Maxim OEM"},
            {"nicolai", "ELAN TTS Russian (Nicolai 16Khz)"},
        };

        private readonly HttpListener _listener;
        private readonly SpeechSynthesizer _engine;
        private readonly int _port = 8080;
        private EventLog Logger { get; set; }

        public Server()
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
            {
                _port = port;
            }

            _listener = new HttpListener {IgnoreWriteExceptions = true};

            var urls = new List<string> {$"http://+:{_port}/"};
            urls.ForEach(url => _listener.Prefixes.Add(url));

            var srcName = ConfigurationManager.AppSettings["eventSourceName"];
            Logger = new EventLog {Source = srcName};

            _engine = new SpeechSynthesizer();

            // Just printing voices info
            var voi = _engine.GetInstalledVoices().ToList().Aggregate("", (acc, v) =>
                acc + $"name: {v.VoiceInfo.Name}; id: {v.VoiceInfo.Id}; enabled: {v.Enabled}\n"
            );
            Logger.WriteEntry(voi);
            Console.WriteLine(voi);
        }

        public void Listen()
        {
            Console.WriteLine("Serving port :{0}", _port);
            var mainLoop = MainLoop();
            mainLoop.Wait();
        }

        private async Task MainLoop()
        {
            _listener.Start();
            for (;;)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    lock (_listener)
                    {
                        ProcessRequest(context);
                    }
                }
                catch (Exception e)
                {
                    if (e is HttpListenerException) return;
                }
            }
        }

        private void ProcessRequest(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            var res = ctx.Response;
            Console.WriteLine($"[{DateTime.Now:yyyy/MM/dd HH:mm:ss.ff}] {req.HttpMethod} {req.Url.AbsolutePath}");

            if (req.HttpMethod.Equals("GET"))
            {
                res.StatusCode = 200;
                res.ContentType = "text/plain";
                var b = Encoding.UTF8.GetBytes("ok");
                res.OutputStream.Write(b, 0, b.Length);
                res.Close();
            }

            if (req.HttpMethod.Equals("POST") && req.Url.AbsolutePath.Equals("/say") &&
                req.Headers["Range"] == null)
            {
                var reader = new JsonTextReader(new StreamReader(req.InputStream));
                var config = new JsonSerializer().Deserialize<ReqBody>(reader);

                if (!_voiceNames.ContainsKey(config.Voice))
                {
                    config.Voice = _voiceNames.Keys.First();
                }

                var tempStream = new MemoryStream();
                _engine.Volume = config.Volume;
                _engine.Rate = config.Rate;
                _engine.SelectVoice(_voiceNames[config.Voice]);
                _engine.SetOutputToWaveStream(tempStream);
                _engine.Speak(config.Phrase);

                tempStream.Seek(0, SeekOrigin.Begin);
                var bytes = tempStream.ToArray();
                tempStream.Close();

                res.StatusCode = 200;
                res.ContentType = "audio/wav";
                res.ContentLength64 = bytes.Length;
                res.OutputStream.Write(bytes, 0, bytes.Length);
                res.OutputStream.Close();
            }

            res.StatusCode = 200;
            res.Close();
        }

        public void Stop()
        {
            _listener.Stop();
        }
    }
}