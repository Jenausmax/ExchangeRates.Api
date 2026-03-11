using ExchangeRatesBot.Configuration.ModelConfig;
using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ExchangeRatesBot.Maintenance.Jobs
{
    /// <summary>
    /// Фоновый сервис для получения обновлений от Telegram в режиме polling.
    /// Используется для локальной разработки и Docker окружений без публичного домена.
    /// </summary>
    public class PollingBackgroundService : BackgroundService
    {
        private readonly IBotService _botService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly BotConfig _config;

        public PollingBackgroundService(
            IBotService botService,
            IServiceProvider serviceProvider,
            IOptions<BotConfig> config,
            ILogger logger)
        {
            _botService = botService;
            _serviceProvider = serviceProvider;
            _config = config.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.Information("Starting Telegram Polling Service...");

            try
            {
                var me = await _botService.Client.GetMeAsync(stoppingToken);
                _logger.Information($"Polling started for bot @{me.Username} (ID: {me.Id})");

                int offset = 0;

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Получаем обновления с текущим offset
                        var updates = await _botService.Client.GetUpdatesAsync(
                            offset: offset,
                            timeout: 30, // Таймаут long polling в секундах
                            allowedUpdates: Array.Empty<UpdateType>(), // Получать все типы обновлений
                            cancellationToken: stoppingToken
                        );

                        // Обрабатываем каждое обновление
                        foreach (var update in updates)
                        {
                            try
                            {
                                _logger.Information($"Received update {update.Id} of type {update.Type}");

                                // Создаем scope для scoped сервисов
                                using var scope = _serviceProvider.CreateScope();
                                var commandService = scope.ServiceProvider.GetRequiredService<ICommandBot>();

                                // Переиспользуем существующую логику
                                await commandService.SetCommandBot(update);

                                _logger.Information($"Successfully processed update {update.Id}");
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(ex, $"Error processing update {update.Id}");
                            }

                            // Обновляем offset для следующего запроса
                            offset = update.Id + 1;
                        }

                        // Небольшая задержка если нет обновлений
                        if (!updates.Any())
                        {
                            await Task.Delay(100, stoppingToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Нормальная остановка сервиса
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error in polling loop");
                        // Задержка перед повторной попыткой при ошибке
                        await Task.Delay(5000, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Polling service is stopping due to cancellation request.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Critical error in Polling Service");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Information("Stopping Telegram Polling Service...");
            await base.StopAsync(cancellationToken);
            _logger.Information("Telegram Polling Service stopped.");
        }
    }
}
