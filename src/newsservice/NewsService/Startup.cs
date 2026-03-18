using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NewsService.App.Services;
using NewsService.Configuration;
using NewsService.DB;
using NewsService.DB.Repositories;
using NewsService.Domain.Interfaces;
using NewsService.Maintenance.Jobs;

namespace NewsService
{
    public class Startup
    {
        public IConfiguration Config { get; }

        public Startup(IConfiguration config)
        {
            Config = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<NewsDataDb>(options =>
                options.UseSqlite(Config.GetConnectionString("NewsDb"),
                    b => b.MigrationsAssembly("NewsService.Migrations")));

            services.Configure<NewsConfig>(Config.GetSection("NewsConfig"));
            services.Configure<LlmConfig>(Config.GetSection("LlmConfig"));

            services.AddScoped<INewsRepository, NewsRepository>();
            services.AddScoped<IRssFetcherService, RssFetcherService>();
            services.AddScoped<INewsDeduplicationService, NewsDeduplicationService>();
            services.AddScoped<INewsDigestService, NewsDigestService>();

            // Регистрация LLM-сервиса в зависимости от конфигурации
            var llmConfig = Config.GetSection("LlmConfig").Get<LlmConfig>();
            var llmProvider = llmConfig?.Provider?.ToLower() ?? "";
            if (llmProvider == "polza")
            {
                Serilog.Log.Information("LLM provider: Polza (model: {Model}, API key set: {HasKey})",
                    llmConfig.PolzaModel, !string.IsNullOrWhiteSpace(llmConfig.PolzaApiKey));
                services.AddSingleton<ILlmService, PolzaLlmService>();
            }
            else if (llmProvider == "ollama")
            {
                Serilog.Log.Information("LLM provider: Ollama (URL: {Url}, model: {Model})",
                    llmConfig.OllamaUrl, llmConfig.OllamaModel);
                services.AddSingleton<ILlmService, OllamaLlmService>();
            }
            else
            {
                Serilog.Log.Information("LLM provider: None (NoopLlmService) — summarization disabled");
                services.AddSingleton<ILlmService, NoopLlmService>();
            }

            // Регистрация фоновой задачи
            var newsConfig = Config.GetSection("NewsConfig").Get<NewsConfig>();
            if (newsConfig == null || newsConfig.Enabled)
            {
                services.AddHostedService<JobsFetchNews>();
            }

            services.AddControllers().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, NewsDataDb dataDb)
        {
            dataDb.Database.Migrate();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
