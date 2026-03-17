using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.App.Services
{
    public class NewsApiClientService : INewsApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public NewsApiClientService(IOptions<BotConfig> config, ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();

            if (!string.IsNullOrWhiteSpace(config.Value.NewsServiceUrl))
            {
                _httpClient.BaseAddress = new Uri(config.Value.NewsServiceUrl);
            }

            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Получить последний дайджест новостей от NewsService
        /// </summary>
        public async Task<NewsDigestResult> GetLatestDigestAsync(int maxNews = 10, CancellationToken cancel = default)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/digest/latest?maxNews={maxNews}", cancel);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancel);
                return JsonSerializer.Deserialize<NewsDigestResult>(json, _jsonOptions)
                       ?? new NewsDigestResult { Message = "", TopicIds = new List<int>() };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get digest from NewsService");
                return new NewsDigestResult { Message = "", TopicIds = new List<int>() };
            }
        }

        /// <summary>
        /// Пометить темы как отправленные
        /// </summary>
        public async Task MarkSentAsync(List<int> topicIds, CancellationToken cancel = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(new { TopicIds = topicIds });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/digest/mark-sent", content, cancel);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to mark topics as sent");
            }
        }

        /// <summary>
        /// Получить статус NewsService
        /// </summary>
        public async Task<NewsServiceStatus> GetStatusAsync(CancellationToken cancel = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/digest/status", cancel);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(cancel);
                return JsonSerializer.Deserialize<NewsServiceStatus>(json, _jsonOptions)
                       ?? new NewsServiceStatus();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to get NewsService status");
                return new NewsServiceStatus();
            }
        }
    }
}
