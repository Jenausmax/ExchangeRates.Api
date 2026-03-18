using NewsService.App.Helpers;
using NewsService.Domain.Interfaces;
using NewsService.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.App.Services
{
    public class NewsDeduplicationService : INewsDeduplicationService
    {
        private readonly INewsRepository _repository;
        private readonly ILlmService _llmService;
        private readonly ILogger _logger;

        public NewsDeduplicationService(INewsRepository repository, ILlmService llmService, ILogger logger)
        {
            _repository = repository;
            _llmService = llmService;
            _logger = logger;
        }

        public async Task<List<NewsTopicDb>> DeduplicateAndSaveAsync(List<RssNewsItem> items, CancellationToken cancel = default)
        {
            var savedTopics = new List<NewsTopicDb>();

            foreach (var item in items)
            {
                try
                {
                    var normalizedTitle = NewsNormalizationHelper.NormalizeTitle(item.Title);
                    var hash = NewsNormalizationHelper.ComputeHash(item.Title, item.Url);

                    if (await _repository.ExistsByHashAsync(hash, cancel))
                    {
                        _logger.Debug("Duplicate skipped: {Title}", normalizedTitle);
                        continue;
                    }

                    var summary = NewsNormalizationHelper.StripHtml(item.Description);
                    summary = NewsNormalizationHelper.TruncateText(summary);

                    if (!_llmService.IsAvailable)
                    {
                        _logger.Debug("LLM not available for: {Title}, using plain text", normalizedTitle);
                    }

                    // Пробуем LLM-суммаризацию если доступна
                    if (_llmService.IsAvailable && !string.IsNullOrWhiteSpace(item.Description))
                    {
                        _logger.Debug("LLM is available, attempting summarization for: {Title}", normalizedTitle);
                        try
                        {
                            var llmSummary = await _llmService.SummarizeAsync(
                                $"{normalizedTitle}\n\n{NewsNormalizationHelper.StripHtml(item.Description)}", cancel);
                            if (!string.IsNullOrWhiteSpace(llmSummary))
                                summary = llmSummary;
                        }
                        catch (Exception ex)
                        {
                            _logger.Warning(ex, "LLM summarization failed, using plain text");
                        }
                    }

                    var topic = new NewsTopicDb
                    {
                        Title = normalizedTitle,
                        Summary = summary,
                        Url = item.Url,
                        Source = ExtractSource(item.SourceFeed),
                        PublishedAt = item.PublishedAt,
                        FetchedAt = DateTime.UtcNow,
                        IsSent = false,
                        ContentHash = hash
                    };

                    topic = await _repository.CreateTopicAsync(topic, cancel);

                    var newsItem = new NewsItemDb
                    {
                        TopicId = topic.Id,
                        RawTitle = item.Title,
                        RawDescription = item.Description,
                        RawUrl = item.Url,
                        RawPublishedAt = item.PublishedAt,
                        SourceFeed = item.SourceFeed
                    };

                    await _repository.CreateItemAsync(newsItem, cancel);
                    savedTopics.Add(topic);

                    _logger.Information("Saved new topic: {Title}", normalizedTitle);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process news item: {Title}", item.Title);
                }
            }

            return savedTopics;
        }

        private string ExtractSource(string feedUrl)
        {
            try
            {
                var uri = new Uri(feedUrl);
                return uri.Host;
            }
            catch
            {
                return feedUrl;
            }
        }
    }
}
