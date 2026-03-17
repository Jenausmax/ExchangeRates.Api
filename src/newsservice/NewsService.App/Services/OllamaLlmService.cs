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
    public class OllamaLlmService : ILlmService
    {
        private readonly LlmConfig _config;
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public bool IsAvailable
        {
            get
            {
                try
                {
                    return !string.IsNullOrWhiteSpace(_config.OllamaUrl);
                }
                catch
                {
                    return false;
                }
            }
        }

        public OllamaLlmService(IOptions<LlmConfig> config, ILogger logger)
        {
            _config = config.Value;
            _logger = logger;
            _httpClient = new HttpClient();
            if (!string.IsNullOrWhiteSpace(_config.OllamaUrl))
            {
                _httpClient.BaseAddress = new Uri(_config.OllamaUrl);
            }
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
        }

        public async Task<string> SummarizeAsync(string text, CancellationToken cancel = default)
        {
            if (!IsAvailable) return null;

            var request = new
            {
                model = _config.OllamaModel,
                prompt = $"Кратко резюмируй новость на русском языке в 1-2 предложения. Только суть, без вводных слов.\n\n{text}",
                stream = false
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/generate", content, cancel);
            var responseJson = await response.Content.ReadAsStringAsync(cancel);

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            return (string)result?.response;
        }

        public async Task<bool> AreSimilarAsync(string text1, string text2, CancellationToken cancel = default)
        {
            if (!IsAvailable) return false;

            var request = new
            {
                model = _config.OllamaModel,
                prompt = $"Определи, являются ли две новости об одном и том же событии. Ответь только 'да' или 'нет'.\n\nНовость 1: {text1}\n\nНовость 2: {text2}",
                stream = false
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/generate", content, cancel);
            var responseJson = await response.Content.ReadAsStringAsync(cancel);

            dynamic result = JsonConvert.DeserializeObject(responseJson);
            var answer = ((string)result?.response)?.ToLower()?.Trim();
            return answer == "да";
        }
    }
}
