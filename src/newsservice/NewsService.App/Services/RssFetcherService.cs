using Microsoft.Extensions.Options;
using NewsService.Configuration;
using NewsService.Domain.Interfaces;
using NewsService.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace NewsService.App.Services
{
    public class RssFetcherService : IRssFetcherService
    {
        private readonly NewsConfig _config;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public RssFetcherService(IOptions<NewsConfig> config, ILogger logger)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<List<RssNewsItem>> FetchAllFeedsAsync(CancellationToken cancel = default)
        {
            var allItems = new List<RssNewsItem>();

            foreach (var feedUrl in _config.RssFeeds)
            {
                try
                {
                    var items = await FetchFeedAsync(feedUrl, cancel);
                    allItems.AddRange(items);
                    _logger.Information("Fetched {Count} items from {Feed}", items.Count, feedUrl);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to fetch RSS feed: {Feed}", feedUrl);
                }
            }

            return allItems;
        }

        private async Task<List<RssNewsItem>> FetchFeedAsync(string feedUrl, CancellationToken cancel)
        {
            var items = new List<RssNewsItem>();
            var response = await _httpClient.GetStringAsync(feedUrl, cancel);

            var doc = new XmlDocument();
            doc.LoadXml(response);

            // RSS 2.0 формат
            var itemNodes = doc.SelectNodes("//item");
            if (itemNodes != null)
            {
                foreach (XmlNode node in itemNodes)
                {
                    var item = ParseRssItem(node, feedUrl);
                    if (item != null)
                        items.Add(item);
                }
            }

            return items;
        }

        private RssNewsItem ParseRssItem(XmlNode node, string feedUrl)
        {
            try
            {
                var title = node.SelectSingleNode("title")?.InnerText;
                var description = node.SelectSingleNode("description")?.InnerText;
                var link = node.SelectSingleNode("link")?.InnerText;
                var pubDate = node.SelectSingleNode("pubDate")?.InnerText;

                DateTime publishedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(pubDate))
                {
                    DateTime.TryParse(pubDate, out publishedAt);
                }

                return new RssNewsItem
                {
                    Title = title ?? "",
                    Description = description ?? "",
                    Url = link ?? "",
                    PublishedAt = publishedAt,
                    SourceFeed = feedUrl
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
