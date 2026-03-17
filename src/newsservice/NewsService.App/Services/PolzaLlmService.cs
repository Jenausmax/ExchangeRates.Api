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
            }
        }

        public async Task<string> SummarizeAsync(string text, CancellationToken cancel = default)
        {
            if (!IsAvailable) return null;

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

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            return (string)result?.choices?[0]?.message?.content;
        }

        public async Task<bool> AreSimilarAsync(string text1, string text2, CancellationToken cancel = default)
        {
            if (!IsAvailable) return false;

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

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            var answer = ((string)result?.choices?[0]?.message?.content)?.ToLower()?.Trim();
            return answer == "да";
        }
    }
}
