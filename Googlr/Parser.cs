namespace Googlr
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using System.Xml.XPath;

    using Flurl.Http;

    using HtmlAgilityPack;

    internal class Parser
    {
        private const string ParserConfigUrl = "https://vtp.blob.core.windows.net/googlr/googlr.json";
        private const string NewsPrefix = "https://news.google.com";
        private const string Href = "href";
        dynamic config;

        internal async Task Init()
        {
            config = await ParserConfigUrl.GetJsonAsync();
        }

        internal async Task<List<SearchInfo>> GetSearchResults(string searchPhrase)
        {
            var searchUrl = this.config.Search.SearchUrl + searchPhrase;
            var results = await this.GetResults(searchUrl, config.Search);
            return results;
        }

        internal async Task<List<SearchInfo>> GetNewsResults(string searchPhrase)
        {
            var searchUrl = this.config.News.SearchUrl + searchPhrase;
            var results = await this.GetResults(searchUrl, config.News, NewsPrefix);
            return results;
        }

        internal async Task<List<string>> GetSummary(string url)
        {
            var web = new HtmlWeb();
            web.PreRequest = request =>
            {
                // request.MaximumAutomaticRedirections = 1;
                request.AllowAutoRedirect = true;
                return true;
            };
            var doc = await web.LoadFromWebAsync(url);
            var results = doc.DocumentNode.SelectNodes("//p")?.Where(p => p.InnerText?.Trim()?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)?.Length >= 10).Select(p => HttpUtility.HtmlDecode(p.InnerText?.Trim())).ToList();
            return results;
        }

        private async Task<List<SearchInfo>> GetResults(string searchUrl, dynamic config, string linkPrefix = "")
        {
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(searchUrl);
            HtmlNodeCollection rows = doc.DocumentNode.SelectNodes(config.Main);
            var results = new List<SearchInfo>();
            foreach (var row in rows)
            {
                var title = row.SelectSingleNode(config.Title);
                var summary = row.SelectSingleNode(config.Summary)?.InnerText ?? string.Empty;
                var time = string.Empty;
                try
                {
                    time = row.SelectSingleNode(config.Time)?.InnerText ?? string.Empty;
                }
                catch (XPathException)
                {
                    //  Do nothing
                }

                if (title != null)
                {
                    results.Add(new SearchInfo
                    {
                        Link = linkPrefix + (title?.GetAttributeValue(Href, string.Empty) ?? string.Empty)?.TrimStart('.'),
                        Title = string.IsNullOrWhiteSpace(title?.InnerText) ? string.Empty : HttpUtility.HtmlDecode(title?.InnerText.Trim()),
                        Summary = string.IsNullOrWhiteSpace(summary) ? string.Empty : HttpUtility.HtmlDecode(summary.Trim()),
                        Time = time ?? string.Empty
                    });
                }
            }

            return await Task.FromResult(results);
        }
    }
}
