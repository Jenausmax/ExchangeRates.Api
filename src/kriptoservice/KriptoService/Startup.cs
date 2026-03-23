using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using KriptoService.App.Services;
using KriptoService.Configuration;
using KriptoService.DB;
using KriptoService.DB.Repositories;
using KriptoService.Domain.Interfaces;
using KriptoService.Maintenance.Jobs;

namespace KriptoService
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
            services.AddDbContext<KriptoDataDb>(options =>
                options.UseSqlite(Config.GetConnectionString("KriptoDb"),
                    b => b.MigrationsAssembly("KriptoService.Migrations")));

            services.Configure<KriptoConfig>(Config.GetSection("KriptoConfig"));

            services.AddScoped<ICryptoRepository, CryptoRepository>();
            services.AddHttpClient<ICryptoFetcherService, CryptoCompareFetcherService>();
            services.AddScoped<ICryptoService, CryptoService>();

            // Регистрация фоновой задачи
            var kriptoConfig = Config.GetSection("KriptoConfig").Get<KriptoConfig>();
            if (kriptoConfig == null || kriptoConfig.Enabled)
            {
                services.AddHostedService<JobsFetchCrypto>();
            }

            services.AddControllers().AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, KriptoDataDb dataDb)
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
