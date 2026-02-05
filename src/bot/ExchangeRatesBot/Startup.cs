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
            services.AddScoped<ICommandBot, CommandService>();
            services.AddScoped<IApiClient, ApiClientService>();
            services.AddScoped<IMessageValute, MessageValuteService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped(typeof(IBaseRepositoryDb<>), (typeof(RepositoryDb<>)));

            services.Configure<BotConfig>(Config.GetSection("BotConfig"));

            services.AddHostedService<JobsSendMessageUsers>();

            services.AddControllers().AddNewtonsoftJson();
        }


        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
