namespace NewsService.Configuration
{
    public class LlmConfig
    {
        public string Provider { get; set; } = ""; // "", "polza", "ollama"
        public string PolzaApiKey { get; set; } = "";
        public string PolzaModel { get; set; } = "polza-1";
        public string OllamaUrl { get; set; } = "http://localhost:11434";
        public string OllamaModel { get; set; } = "llama3.2";
    }
}
