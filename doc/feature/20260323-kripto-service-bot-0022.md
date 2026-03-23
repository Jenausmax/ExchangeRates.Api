# BOT-0022: KriptoService — микросервис криптовалют (костяк)

- [ ] Не реализовано

## Описание

Четвёртый микросервис в монорепозитории — KriptoService. Получает курсы криптовалют с CryptoCompare API, хранит историю в SQLite, предоставляет REST API для выдачи курсов.

## Источник данных

**CryptoCompare API** (`https://min-api.cryptocompare.com/data/pricemultifull`)
- 100K вызовов/мес, прямая поддержка RUB
- Топ-10 монет: BTC, ETH, SOL, XRP, BNB, USDT, DOGE, ADA, TON, AVAX
- Интервал опроса: 5 мин (~8640 запросов/мес)

## Структура

7 проектов в `src/kriptoservice/` по паттерну NewsService:
- KriptoService (Web Host)
- KriptoService.App (сервисы)
- KriptoService.Configuration (KriptoConfig)
- KriptoService.DB (DbContext, репозиторий)
- KriptoService.Domain (модели, DTO, интерфейсы)
- KriptoService.Maintenance (фоновые задачи)
- KriptoService.Migrations (EF миграции)

## API

- `GET /api/crypto/latest` — последние курсы
- `GET /api/crypto/history` — история за N часов
- `GET /api/crypto/status` — статус сервиса

## Docker

4-й контейнер `exchangerates-kripto` на порту 5003.
