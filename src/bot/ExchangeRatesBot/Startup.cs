using ExchangeRatesBot.App.Handlers;
using ExchangeRatesBot.App.Services;
using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.DB;
using ExchangeRatesBot.DB.Repositories;
using ExchangeRatesBot.Domain.Interfaces;
using ExchangeRatesBot.Maintenance.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExchangeRatesBot
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
            services.AddDbContext<DataDb>(options =>
                options.UseSqlite(Config.GetConnectionString("SqliteConnection"))
                       .UseSqlite(sqliteOptionsAction: b => b.MigrationsAssembly("ExchangeRatesBot.Migrations")));

            services.AddSingleton<IBotService, BotService>();
            services.AddScoped<IUpdateService, UpdateService>();
            services.AddScoped<IProcessingService, ProcessingService>();
            // BOT-0025: Singleton для in-memory состояния выбора
            services.AddSingleton<IUserSelectionState, UserSelectionState>();

            // BOT-0025: Доменные handler'ы
            services.AddScoped<StartHandler>();
            services.AddScoped<ValuteHandler>();
            services.AddScoped<CurrenciesHandler>();
            services.AddScoped<StatisticsHandler>();
            services.AddScoped<SubscriptionHandler>();
            services.AddScoped<NewsHandler>();
            services.AddScoped<CryptoHandler>();

            services.AddScoped<ICommandBot, CommandService>();
            services.AddScoped<IApiClient, ApiClientService>();
            services.AddScoped<IMessageValute, MessageValuteService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<INewsApiClient, NewsApiClientService>();
            services.AddScoped<IKriptoApiClient, KriptoApiClientService>();
            services.AddScoped(typeof(IBaseRepositoryDb<>), (typeof(RepositoryDb<>)));

            services.Configure<BotConfig>(Config.GetSection("BotConfig"));

            services.AddHostedService<JobsSendMessageUsers>();

            // Условная регистрация Polling сервиса
            var botConfig = Config.GetSection("BotConfig").Get<BotConfig>();
            if (botConfig.UsePolling)
            {
                services.AddHostedService<PollingBackgroundService>();
            }

            // Условная регистрация новостного дайджеста
            if (botConfig.NewsEnabled)
            {
                services.AddHostedService<JobsSendNewsDigest>();
            }

            // Условная регистрация рассылки важных новостей
            if (botConfig.ImportantNewsEnabled)
            {
                services.AddHostedService<JobsSendImportantNews>();
            }

            services.AddControllers().AddNewtonsoftJson();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataDb dataDb)
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
