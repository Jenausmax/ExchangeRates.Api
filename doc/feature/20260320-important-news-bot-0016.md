- [x] Реализовано

# BOT-0016: Подписка на важные новости

## Контекст

Новая фича: **одна самая важная новость раз в час**, где важность определяется количеством упоминаний в разных СМИ. Это отдельная подписка, параллельная существующему дайджесту.

**Ключевая проблема:** `ContentHash = SHA256(title + url)` — одна и та же новость из разных источников (разные URL) создавала разные записи. Решение: группировка по Jaccard-similarity на символьных триграммах.

## Ветка: `feature/BOT-0016-important-news`

---

## Часть 1: NewsService — группировка похожих новостей

### 1.1 Поле SourceCount в NewsTopicDb

`src/newsservice/NewsService.Domain/Models/NewsTopicDb.cs`

Добавлено `SourceCount` (int, default 1) — количество источников, упомянувших эту новость.

### 1.2 Индекс по SourceCount

`src/newsservice/NewsService.DB/NewsDataDb.cs`

Индекс `IX_Topics_SourceCount` для эффективной сортировки по важности.

### 1.3 Алгоритм схожести (Jaccard на символьных триграммах)

`src/newsservice/NewsService.App/Helpers/NewsNormalizationHelper.cs`

Три новых метода:
- `NormalizeTitleForSimilarity(title)` — lowercase, убрать пунктуацию и 30+ русских стоп-слов
- `GetCharNGrams(text, n=3)` → `HashSet<string>` символьных триграмм
- `JaccardSimilarity(set1, set2)` → `double` (пересечение / объединение)

**Почему не LLM:** character n-grams бесплатны, быстры и хорошо работают для кириллицы.

### 1.4 Новые методы репозитория

`src/newsservice/NewsService.Domain/Interfaces/INewsRepository.cs`
`src/newsservice/NewsService.DB/Repositories/NewsRepository.cs`

- `GetRecentTopicsForSimilarityAsync(hoursBack=48)` — все темы за 48 часов для in-memory сравнения
- `GetMostImportantUnsentAsync()` — одна самая важная неотправленная: `OrderByDescending(SourceCount).ThenByDescending(PublishedAt).First()`
- `IncrementSourceCountAsync(topicId)` — загрузить + SourceCount++ + Save

### 1.5 Изменённая дедупликация

`src/newsservice/NewsService.App/Services/NewsDeduplicationService.cs`

Новый алгоритм:
1. Проверить `ExistsByHashAsync(hash)` — точный дубликат → skip
2. Загрузить `GetRecentTopicsForSimilarityAsync(48)`
3. Вычислить `JaccardSimilarity` для каждого существующего topic
4. Если max Jaccard >= порог (default 0.5):
   - НЕ создавать новый Topic
   - `IncrementSourceCountAsync(matchedTopic.Id)`
   - Создать `NewsItemDb` привязанный к существующему Topic
5. Иначе — создать новый Topic с `SourceCount = 1`

Конструктор расширен: добавлен `IOptions<NewsConfig>` для доступа к `SimilarityThreshold`.

### 1.6 Конфигурация

`src/newsservice/NewsService.Configuration/NewsConfig.cs`

- `SimilarityThreshold` (double, default 0.5) — порог Jaccard-similarity
- `FetchIntervalMinutes` изменён с 60 на 30 (чаще парсим RSS для лучшего определения важности)

### 1.7 Эндпоинт GET /api/digest/top

`src/newsservice/NewsService.Domain/Interfaces/INewsDigestService.cs` — метод `GetMostImportantAsync()`
`src/newsservice/NewsService.App/Services/NewsDigestService.cs` — реализация + `FormatImportantNewsMessage()` с указанием "Упоминаний в СМИ: N"
`src/newsservice/NewsService/Controllers/DigestController.cs` — `[HttpGet("top")]`

### 1.8 EF-миграция

`AddSourceCount` — столбец `SourceCount INTEGER` + индекс `IX_Topics_SourceCount`

---

## Часть 2: Bot — подписка и рассылка важных новостей

### 2.1 Новые поля в UserDb

`src/bot/ExchangeRatesBot.DB/Models/UserDb.cs`

- `ImportantNewsSubscribe` (bool) — подписка на важные новости
- `LastImportantNewsAt` (DateTime?, nullable) — время последней доставки важной новости

### 2.2 Новые методы UserService

`src/bot/ExchangeRatesBot.Domain/Interfaces/IUserService.cs`
`src/bot/ExchangeRatesBot.App/Services/UserService.cs`

- `ImportantNewsSubscribeUpdate(chatId, subscribe)`
- `UpdateLastImportantNewsAt(chatId, deliveredAt)`

### 2.3 NewsApiClientService — GetMostImportantAsync

`src/bot/ExchangeRatesBot.Domain/Interfaces/INewsApiClient.cs`
`src/bot/ExchangeRatesBot.App/Services/NewsApiClientService.cs`

Новый метод вызывает `GET /api/digest/top`.

### 2.4 Фоновая задача JobsSendImportantNews

`src/bot/ExchangeRatesBot.Maintenance/Jobs/JobsSendImportantNews.cs`

Логика (каждую минуту проверяет, наступил ли XX:00):
1. Найти пользователей с `ImportantNewsSubscribe == true`
2. `GetMostImportantAsync()` → одна новость (один запрос для всех)
3. Отправить каждому пользователю
4. Обновить `LastImportantNewsAt = DateTime.UtcNow`

**Не помечает IsSent** — чтобы не мешать дайджесту.

### 2.5 Конфигурация

`src/bot/ExchangeRatesBot.Configuration/ModelConfig/BotConfig.cs` — `ImportantNewsEnabled` (bool, default false)
`src/bot/ExchangeRatesBot/Startup.cs` — условная регистрация `JobsSendImportantNews`

### 2.6 Inline-меню

`src/bot/ExchangeRatesBot.App/Services/CommandService.cs`

- В `NewsMenu()` добавлена строка: «Важные новости: вкл» / «Важные новости: выкл»
- Callbacks: `important_news_subscribe`, `important_news_unsubscribe`

### 2.7 Фразы

`src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs`

- `ImportantNewsSubscribeTrue` — подтверждение подписки
- `ImportantNewsSubscribeFalse` — подтверждение отписки
- `ImportantNewsAlreadySubscribed` — уже подписан

### 2.8 EF-миграция бота

`AddImportantNewsSubscribe` — столбцы `ImportantNewsSubscribe` (bool) и `LastImportantNewsAt` (datetime?)

---

## Docker / .env

- `docker-compose.yml` — переменная `BotConfig__ImportantNewsEnabled=${IMPORTANT_NEWS_ENABLED:-false}`
- `.env.example` — `IMPORTANT_NEWS_ENABLED=false`
- `appsettings.json` (бот) — `"ImportantNewsEnabled": false`
- `appsettings.json` (NewsService) — `FetchIntervalMinutes: 30`

## Верификация

1. `dotnet build src/ExchangeRates.Api.sln` — 0 ошибок
2. Миграции NewsService и Bot созданы
3. Эндпоинт `GET /api/digest/top` возвращает одну новость с максимальным SourceCount
4. В боте: Новости → «Важные новости: вкл» → подписка оформлена → каждый час приходит одна новость
