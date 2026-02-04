using ExchangeRates.Configuration;
using ExchangeRates.Core.App.Services;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Infrastructure.DB;
using ExchangeRates.Infrastructure.SQLite.Repositories;
using ExchangeRates.Maintenance.Jobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ExchangeRates.Api
{
    public class Startup
    {
        public Startup(IConfiguration config)
        {
            Configuration = config;
        }
        private IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddDbContext<DataDb>(options =>
                options.UseSqlite(Configuration.GetConnectionString("DbData"))
                       .UseSqlite(sqliteOptionsAction: b => b.MigrationsAssembly("ExchangeRates.Migrations")));


            services.AddScoped<IApiClient, ApiClientService>();
            services.AddScoped<IProcessingService, ProcessingService>();
            services.AddScoped<ISaveService, SaveService>();
            services.AddTransient<IGetValute, GetValuteService>();
            services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryDbSQLite<>));
            
            services.Configure<ClientConfig>(Configuration.GetSection("ClientConfig"));
            var con = Configuration["ClientConfig:JobsValute"];
            var conJobsToHour = Configuration["ClientConfig:JobsValuteToHour"];

            if (con == "True")
            {
                services.AddHostedService<JobsCreateValute>();
            }
            if (conJobsToHour == "True")
            {
                services.AddHostedService<JobsCreateValuteToHour>();
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataDb dataDb)
        {
            // Автоматическое применение миграций при старте
            dataDb.Database.Migrate();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSerilogRequestLogging();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
