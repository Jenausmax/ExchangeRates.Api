using Microsoft.Extensions.Options;
using NewsService.Configuration;
using NewsService.Domain.Dto;
using NewsService.Domain.Interfaces;
using NewsService.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.App.Services
{
    public class NewsDigestService : INewsDigestService
    {
        private readonly INewsRepository _repository;
        private readonly NewsConfig _config;
        private readonly ILogger _logger;

        public NewsDigestService(INewsRepository repository, IOptions<NewsConfig> config, ILogger logger)
        {
            _repository = repository;
            _config = config.Value;
            _logger = logger;
        }

        public async Task<DigestResponse> GetLatestDigestAsync(int maxNews, bool all = false, CancellationToken cancel = default)
        {
            var count = maxNews > 0 ? maxNews : _config.MaxNewsPerDigest;
            // all=true — все новости (для интерактивного просмотра), false — только неотправленные (для рассылки)
            var topics = all
                ? await _repository.GetAllTopicsAsync(count + 1, cancel)
                : await _repository.GetUnsentTopicsAsync(count + 1, cancel);
            var hasMore = topics.Count > count;
            if (hasMore)
                topics = topics.Take(count).ToList();

            if (!topics.Any())
            {
                return new DigestResponse
                {
                    Message = "",
                    TopicIds = new List<int>(),
                    HasMore = false
                };
            }

            var message = FormatDigestMessage(topics);
            var topicIds = topics.Select(t => t.Id).ToList();

            return new DigestResponse
            {
                Message = message,
                TopicIds = topicIds,
                HasMore = hasMore
            };
        }

        public async Task<DigestResponse> GetDigestSinceAsync(DateTime since, int maxNews, CancellationToken cancel = default)
        {
            var count = maxNews > 0 ? maxNews : _config.MaxNewsPerDigest;
            var topics = await _repository.GetTopicsSinceAsync(since, count + 1, cancel);
            var hasMore = topics.Count > count;
            if (hasMore)
                topics = topics.Take(count).ToList();

            if (!topics.Any())
            {
                return new DigestResponse
                {
                    Message = "",
                    TopicIds = new List<int>(),
                    HasMore = false
                };
            }

            var message = FormatDigestMessage(topics);
            var topicIds = topics.Select(t => t.Id).ToList();

            return new DigestResponse
            {
                Message = message,
                TopicIds = topicIds,
                HasMore = hasMore
            };
        }

        public async Task<DigestResponse> GetDigestBeforeIdAsync(int beforeId, int maxNews, CancellationToken cancel = default)
        {
            var count = maxNews > 0 ? maxNews : _config.MaxNewsPerDigest;
            var topics = await _repository.GetTopicsBeforeIdAsync(beforeId, count + 1, cancel);
            var hasMore = topics.Count > count;
            if (hasMore)
                topics = topics.Take(count).ToList();

            if (!topics.Any())
            {
                return new DigestResponse
                {
                    Message = "",
                    TopicIds = new List<int>(),
                    HasMore = false
                };
            }

            var message = FormatDigestMessage(topics, "Продолжение");
            var topicIds = topics.Select(t => t.Id).ToList();

            return new DigestResponse
            {
                Message = message,
                TopicIds = topicIds,
                HasMore = hasMore
            };
        }

        public async Task<int> MarkAsSentAsync(List<int> topicIds, CancellationToken cancel = default)
        {
            var marked = await _repository.MarkTopicsAsSentAsync(topicIds, cancel);
            _logger.Information("Marked {Count} topics as sent (requested {Requested})", marked, topicIds.Count);
            return marked;
        }

        public async Task<ServiceStatusResponse> GetStatusAsync(CancellationToken cancel = default)
        {
            return new ServiceStatusResponse
            {
                TotalTopics = await _repository.GetTotalCountAsync(cancel),
                UnsentTopics = await _repository.GetUnsentCountAsync(cancel),
                LastFetchTime = await _repository.GetLastFetchTimeAsync(cancel)
            };
        }

        private string FormatDigestMessage(List<NewsTopicDb> topics, string header = null)
        {
            var sb = new StringBuilder();
            var title = string.IsNullOrWhiteSpace(header) ? "Новостной дайджест" : header;
            sb.AppendLine($"*{title}* \U0001F4F0\n");

            for (int i = 0; i < topics.Count; i++)
            {
                var topic = topics[i];
                sb.AppendLine($"*{i + 1}. {EscapeMarkdown(topic.Title)}*");

                if (!string.IsNullOrWhiteSpace(topic.Summary))
                {
                    sb.AppendLine(EscapeMarkdown(topic.Summary));
                }

                if (!string.IsNullOrWhiteSpace(topic.Url))
                {
                    sb.AppendLine($"[Читать далее]({topic.Url})");
                }

                sb.AppendLine($"_{topic.Source} \u2022 {topic.PublishedAt:dd.MM.yyyy}_\n");
            }

            return sb.ToString();
        }

        private string EscapeMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return text
                .Replace("_", "\\_")
                .Replace("*", "\\*")
                .Replace("[", "\\[")
                .Replace("`", "\\`");
        }
    }
}
