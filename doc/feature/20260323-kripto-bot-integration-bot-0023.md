- [x] Реализовано

# BOT-0023: Интеграция KriptoService с Telegram-ботом

## Описание

Интеграция микросервиса криптовалют (KriptoService, BOT-0022) с Telegram-ботом.
Пользователи могут просматривать курсы топ-10 криптовалют прямо в боте.

## Реализовано

### Кнопка «Крипто» в reply-клавиатуре
- Добавлена в 3-й ряд рядом с «Новости»
- Также доступна команда `/crypto`

### Отображение курсов
- Топ-10 монет: BTC, ETH, SOL, XRP, BNB, USDT, DOGE, ADA, TON, AVAX
- Цена + изменение за 24ч в процентах (🟢/🔴)
- Символ валюты: ₽ для RUB, $ для USD

### Inline-кнопки
- `[RUB]` / `[USD]` — переключение валюты (активная выделена скобками)
- `🔄 Обновить` — перезапросить данные

### HTTP-клиент
- `IKriptoApiClient` + `KriptoApiClientService` — по паттерну NewsApiClientService
- Graceful degradation при недоступности сервиса

### Конфигурация
- `BotConfig.KriptoServiceUrl` — URL KriptoService
- Docker: `http://exchangerates-kripto:80/`

## Файлы

### Новые
- `src/bot/ExchangeRatesBot.Domain/Interfaces/IKriptoApiClient.cs` — интерфейс + DTO
- `src/bot/ExchangeRatesBot.App/Services/KriptoApiClientService.cs` — HTTP-клиент

### Изменённые
- `src/bot/ExchangeRatesBot.Configuration/ModelConfig/BotConfig.cs` — KriptoServiceUrl
- `src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs` — BtnCrypto, CryptoEmpty, CryptoNames, HelpMessage
- `src/bot/ExchangeRatesBot.App/Services/CommandService.cs` — /crypto, callback crypto_*, HandleCryptoCommand
- `src/bot/ExchangeRatesBot/Startup.cs` — DI IKriptoApiClient
- `src/bot/ExchangeRatesBot/appsettings.json` — KriptoServiceUrl
- `docker-compose.yml` — depends_on kripto, BotConfig__KriptoServiceUrl
- `CLAUDE.md` — документация KriptoService, обновление всех разделов
