namespace Googlr
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web;

    using Flurl.Http;

    using HtmlAgilityPack;

    internal class Parser
    {
        private const string ParserConfigUrl = "https://vtp.blob.core.windows.net/googlr/googlr.json";

        internal async Task<List<(string link, string title, string summary)>> GetSearchResults(string searchPhrase)
        {
            dynamic config = await ParserConfigUrl.GetJsonAsync();

            var cloudUrl = $"http://google.com/search?q={searchPhrase}";
            var web = new HtmlWeb();
            var doc = web.Load(cloudUrl);
            var rows = doc.DocumentNode.SelectNodes(config.Main);
            var results = new List<(string link, string title, string summary)>();

            foreach (var row in rows)
            {
                var title = row.SelectSingleNode(config.Title);
                var summary = row.SelectSingleNode(config.Summary)?.InnerText ?? string.Empty;
                results.Add((title.GetAttributeValue("href", string.Empty), HttpUtility.HtmlDecode(title.InnerText), HttpUtility.HtmlDecode(summary)));
            }

            return results;
        }
    }
}
