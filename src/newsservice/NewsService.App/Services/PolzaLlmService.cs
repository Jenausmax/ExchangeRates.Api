using Microsoft.Extensions.Options;
using NewsService.Configuration;
using NewsService.Domain.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.App.Services
{
    public class PolzaLlmService : ILlmService
    {
        private readonly LlmConfig _config;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public bool IsAvailable => !string.IsNullOrWhiteSpace(_config.PolzaApiKey);

        public PolzaLlmService(IOptions<LlmConfig> config, ILogger logger)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.polza.ai/");
            if (IsAvailable)
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.PolzaApiKey}");
                _logger.Information("PolzaLlmService initialized. Model: {Model}, API key set: true", _config.PolzaModel);
            }
            else
            {
                _logger.Warning("PolzaLlmService initialized but API key is missing — LLM summarization will be disabled");
            }
        }

        public async Task<string> SummarizeAsync(string text, CancellationToken cancel = default)
        {
            if (!IsAvailable) return null;

            try
            {
                var request = new
                {
                    model = _config.PolzaModel,
                    messages = new[]
                    {
                        new { role = "system", content = "Кратко резюмируй новость на русском языке в 1-2 предложения. Только суть, без вводных слов." },
                        new { role = "user", content = text }
                    },
                    max_tokens = 200
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("v1/chat/completions", content, cancel);
                var responseJson = await response.Content.ReadAsStringAsync(cancel);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Polza API returned HTTP {StatusCode}: {Response}", (int)response.StatusCode, responseJson);
                    return null;
                }

                dynamic result = JsonConvert.DeserializeObject(responseJson);
                var summary = (string)result?.choices?[0]?.message?.content;

                if (string.IsNullOrWhiteSpace(summary))
                {
                    _logger.Warning("Polza API returned empty summary. Response: {Response}", responseJson);
                }

                return summary;
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "HTTP error calling Polza API for summarization");
                return null;
            }
            catch (TaskCanceledException ex) when (!cancel.IsCancellationRequested)
            {
                _logger.Error(ex, "Polza API request timed out");
                return null;
            }
        }

        public async Task<bool> AreSimilarAsync(string text1, string text2, CancellationToken cancel = default)
        {
            if (!IsAvailable) return false;

            try
            {
                var request = new
                {
                    model = _config.PolzaModel,
                    messages = new[]
                    {
                        new { role = "system", content = "Определи, являются ли две новости об одном и том же событии. Ответь только 'да' или 'нет'." },
                        new { role = "user", content = $"Новость 1: {text1}\n\nНовость 2: {text2}" }
                    },
                    max_tokens = 10
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("v1/chat/completions", content, cancel);
                var responseJson = await response.Content.ReadAsStringAsync(cancel);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.Error("Polza API returned HTTP {StatusCode} for similarity check: {Response}", (int)response.StatusCode, responseJson);
                    return false;
                }

                dynamic result = JsonConvert.DeserializeObject(responseJson);
                var answer = ((string)result?.choices?[0]?.message?.content)?.ToLower()?.Trim();
                return answer == "да";
            }
            catch (HttpRequestException ex)
            {
                _logger.Error(ex, "HTTP error calling Polza API for similarity check");
                return false;
            }
            catch (TaskCanceledException ex) when (!cancel.IsCancellationRequested)
            {
                _logger.Error(ex, "Polza API similarity request timed out");
                return false;
            }
        }
    }
}
