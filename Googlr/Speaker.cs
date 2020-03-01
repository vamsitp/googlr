namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.CognitiveServices.Speech;
    using Microsoft.Extensions.Configuration;

    public class Speaker : IDisposable
    {
        private readonly IConfiguration config;

        private bool disposedValue = false;

        private bool enableSpeech;

        private SpeechSynthesizer speaker;

        public Speaker(IConfiguration config)
        {
            this.config = config;
            Init();
        }

        private void Init()
        {
            var appSettings = this.config?.GetSection("appSettings");
            if (appSettings != null && !string.IsNullOrWhiteSpace(appSettings["speechSubscriptionKey"]) && !string.IsNullOrWhiteSpace(appSettings["speechRegion"]))
            {
                bool.TryParse(appSettings["enableSpeech"], out enableSpeech);
                var speechConfig = SpeechConfig.FromSubscription(appSettings["speechSubscriptionKey"], appSettings["speechRegion"]);
                if (speechConfig != null)
                {
                    speaker = new SpeechSynthesizer(speechConfig);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task Speak(IEnumerable<SearchInfo> results, CancellationToken stopToken)
        {
            foreach (var result in results)
            {
                if (stopToken.IsCancellationRequested)
                {
                    this.Init();
                }
                else
                {
                    await Speak(result);
                }
            }
        }

        public async Task Speak(SearchInfo result)
        {
            try
            {
                if (this.enableSpeech && speaker != null)
                {
                    var lastIndexOfPeriod = result.Summary.Replace("...", string.Empty).LastIndexOf('.') + 1;
                    if (lastIndexOfPeriod <= 0)
                    {
                        lastIndexOfPeriod = result.Summary.Replace("...", string.Empty).LastIndexOf('?') + 1;
                    }

                    await speaker.SpeakTextAsync((result.Time ?? string.Empty) + ". " + (lastIndexOfPeriod > -1 ? result.Summary.Substring(0, lastIndexOfPeriod) : result.Summary));
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (this.speaker != null)
                    {
                        try
                        {
                            this.speaker.Dispose();
                            this.speaker = null;
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                disposedValue = true;
            }
        }
    }
}
