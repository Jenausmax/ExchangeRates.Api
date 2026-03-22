- [x] Реализовано

# BOT-0019: Ротация важных новостей

## Проблема

Фича BOT-0016 отправляет "самую важную новость" каждый час, но:
1. `GetMostImportantUnsentAsync()` выбирает по `SourceCount DESC` — лидер никогда не меняется
2. `JobsSendImportantNews` не помечает новость как отправленную
3. Нет фильтрации по времени — устаревшая новость (2+ часа) продолжает выбираться

## Решение — 5 изменений, 0 миграций

### 1. NewsConfig — параметр максимального возраста
- Добавлено `ImportantNewsMaxAgeHours = 2` — отсечка устаревших новостей

### 2. INewsRepository — обновлена сигнатура
- Добавлен параметр `int maxAgeHours = 0` в `GetMostImportantUnsentAsync`

### 3. NewsRepository — фильтр по FetchedAt
- При `maxAgeHours > 0` отсекаются новости с `FetchedAt < DateTime.UtcNow.AddHours(-maxAgeHours)`

### 4. NewsDigestService — передача maxAge из конфига
- `GetMostImportantAsync` передаёт `_config.ImportantNewsMaxAgeHours` в репозиторий

### 5. JobsSendImportantNews — пометка отправленной новости
- После успешной рассылки вызывается `newsClient.MarkSentAsync(digest.TopicIds, cancel)`
- Использует уже существующий эндпоинт `POST /api/digest/mark-sent`

## Изменённые файлы

- `src/newsservice/NewsService.Configuration/NewsConfig.cs`
- `src/newsservice/NewsService.Domain/Interfaces/INewsRepository.cs`
- `src/newsservice/NewsService.DB/Repositories/NewsRepository.cs`
- `src/newsservice/NewsService.App/Services/NewsDigestService.cs`
- `src/bot/ExchangeRatesBot.Maintenance/Jobs/JobsSendImportantNews.cs`

## Поведение после реализации

1. Каждый час бот запрашивает `GET /api/digest/top`
2. NewsService ищет неотправленную (`!IsSent`) новость не старше 2ч, с максимальным `SourceCount`
3. Бот рассылает, затем `POST /api/digest/mark-sent` → `IsSent = true`
4. В следующий час выбирается другая новость
5. Если актуальных новостей нет — пустой ответ, бот ничего не шлёт
