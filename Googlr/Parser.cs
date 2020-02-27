namespace Googlr
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Flurl.Http;

    using Newtonsoft.Json.Linq;

    using OpenScraping;
    using OpenScraping.Config;

    using Serilog;

    internal class Parser
    {
        private const string ParserConfigUrl = "./googlr.json"; //"https://vtp.blob.core.windows.net/googlr.json";

        private async Task<ConfigSection> GetParserConfig()
        {
            var jsonConfig = File.ReadAllText(ParserConfigUrl.ToFullPath()); // await ParserConfigUrl.GetJsonAsync();
            var config = StructuredDataConfig.ParseJsonString(jsonConfig);
            return config;
        }

        internal async Task<IEnumerable<string>> GetSearchResults(string searchPhrase)
        {
            var cloudUrl = $"http://google.com/search?q={searchPhrase}";
            var config = await this.GetParserConfig();
            var html = await cloudUrl.GetStringAsync(CancellationToken.None, System.Net.Http.HttpCompletionOption.ResponseContentRead);
            var openScraping = new StructuredDataExtractor(config);
            var scrapingResults = openScraping.Extract(html);
            Debug.Assert(scrapingResults?.Any() == true);
            Log.Debug(scrapingResults.ToString());
            var value = scrapingResults.ToObject<JObject>().SelectToken("..Value")?.Value<string>();
            return new[] { value };
        }
    }
}
