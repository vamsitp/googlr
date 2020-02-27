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
        private const string NewsPrefix = "https://news.google.com/";
        private const string Href = "href";

        internal async Task<List<SearchInfo>> GetSearchResults(string searchPhrase)
        {
            dynamic config = await ParserConfigUrl.GetJsonAsync();
            var searchUrl = config.Search.SearchUrl + searchPhrase;
            var results = await this.GetResults(searchUrl, config.Search);
            return results;
        }

        internal async Task<List<SearchInfo>> GetNewsResults(string searchPhrase)
        {
            dynamic config = await ParserConfigUrl.GetJsonAsync();
            var searchUrl = config.News.SearchUrl + searchPhrase;
            var results = await this.GetResults(searchUrl, config.News);
            return results;
        }

        internal async Task<List<SearchInfo>> GetResults(string searchUrl, dynamic config)
        {
            var web = new HtmlWeb();
            var doc = web.Load(searchUrl);
            var rows = doc.DocumentNode.SelectNodes(config.Main);
            var results = new List<SearchInfo>();
            foreach (var row in rows)
            {
                var title = row.SelectSingleNode(config.Title);
                var summary = row.SelectSingleNode(config.Summary)?.InnerText ?? string.Empty;
                var time = row.SelectSingleNode(config.Time)?.InnerText ?? string.Empty;
                if (title != null)
                {
                    results.Add(new SearchInfo
                    {
                        Link = NewsPrefix + (title?.GetAttributeValue(Href, string.Empty) ?? string.Empty),
                        Title = string.IsNullOrWhiteSpace(title?.InnerText) ? string.Empty : HttpUtility.HtmlDecode(title?.InnerText),
                        Summary = string.IsNullOrWhiteSpace(summary) ? string.Empty : HttpUtility.HtmlDecode(summary),
                        Time = time ?? string.Empty
                    });
                }
            }

            return await Task.FromResult(results);
        }
    }
}
