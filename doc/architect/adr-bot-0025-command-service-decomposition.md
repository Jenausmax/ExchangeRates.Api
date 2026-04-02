# ADR-BOT-0025: Декомпозиция CommandService на доменные handler'ы

**Статус**: Принят и реализован (2026-04-02)
**Дата**: 2026-03-24 (обновлён 2026-04-01)

## Контекст

`CommandService.cs` вырос до 932 строк и продолжает расти с каждой фичей (BOT-0019, BOT-0024, BOT-0026). Класс нарушает SRP: маршрутизация updates, обработка 10+ команд, обработка 30+ inline-callback'ов, хранение in-memory состояния (3 `ConcurrentDictionary`), генерация клавиатур, форматирование данных. Каждая новая фича (крипто, важные новости, персонализация) добавляет 50-100 строк в один файл.

### Проблемы текущего подхода

1. **SRP**: один класс отвечает за маршрутизацию, валюты, подписки, новости, крипто, статистику
2. **Навигация**: 810 строк со switch/if-else -- сложно найти нужный обработчик
3. **Конфликты при мерже**: параллельная работа над разными фичами порождает конфликты в одном файле
4. **Тестируемость**: невозможно протестировать обработку крипто изолированно от новостей
5. **Зависимости**: конструктор принимает 7 сервисов, хотя каждый обработчик использует 2-3 из них
6. **Масштабирование**: каждая новая команда/callback раздувает switch/if-else

### Ограничения

- Telegram.Bot v16.0.2 -- не обновлять
- Все сервисы Scoped (per-request lifetime)
- Static `ConcurrentDictionary` для in-memory состояния -- переживает scope
- `ICommandBot` с единственным методом `SetCommandBot(Update)` -- используется в `UpdateController` и `PollingBackgroundService`
- Нет MediatR и нежелательно добавлять внешние зависимости ради рефакторинга

## Решение

### Паттерн: Command Router + Domain Handlers

`CommandService` остается как **тонкий роутер** (реализует `ICommandBot`), а бизнес-логика выносится в **доменные handler'ы**. Каждый handler -- отдельный класс, отвечающий за один домен.

### Почему не Chain of Responsibility

CoR предполагает, что каждый handler проверяет "могу ли я обработать" и передает дальше. В нашем случае маршрутизация **детерминирована** -- по тексту команды или callback-data всегда однозначно определяется handler. Цепочка добавит overhead без пользы.

### Почему не MediatR

MediatR потребует:
- Новый NuGet-пакет во все bot-проекты
- Определение Request/Response типов для каждой команды
- Pipeline behaviors вместо простых вызовов

Это overengineering для текущего масштаба (6 доменов, 810 строк). Когда handler'ов станет 15+, можно вернуться к этому решению.

### Почему не Strategy

Strategy подходит когда алгоритм выбирается в runtime. Наши handler'ы не взаимозаменяемы -- валютный handler не может заменить крипто-handler. Это просто декомпозиция по доменам.

## Архитектура решения

### Доменные handler'ы (6 штук)

| Handler | Домен | Команды/Callbacks | Зависимости |
|---------|-------|-------------------|-------------|
| `StartHandler` | Приветствие, помощь | `/start`, `/help`, "Помощь" | `IUpdateService` |
| `ValuteHandler` | Курсы валют | `/valuteoneday`, `/valutesevendays`, "Курс сегодня", "За 7 дней" | `IUpdateService`, `IMessageValute`, `IUserService` |
| `CurrenciesHandler` | Персонализация валют | `/currencies`, "Валюты", `toggle_{CODE}`, `save_currencies` | `IUpdateService`, `IBotService`, `IUserService` |
| `StatisticsHandler` | Статистика | `/statistics`, "Статистика", `period_{N}` | `IUpdateService`, `IMessageValute`, `IUserService` |
| `SubscriptionHandler` | Подписки | `/subscribe`, "Подписка", `sub_*`, legacy callbacks | `IUpdateService`, `IBotService`, `IUserService` |
| `NewsHandler` | Новости, расписание | `/news`, "Новости", `news_*`, `toggle_news_*`, `save_news_schedule` | `IUpdateService`, `IBotService`, `IUserService`, `INewsApiClient` |
| `CryptoHandler` | Криптовалюты | `/crypto`, `/cryptocoins`, "Крипто", "Монеты", `crypto_*`, `toggle_crypto_*`, `save_crypto_coins` | `IUpdateService`, `IBotService`, `IUserService`, `IKriptoApiClient`, `IUserSelectionState` |

### Общие зависимости

- `IUpdateService` -- нужен всем (отправка сообщений)
- `IBotService` -- нужен handler'ам, которые редактируют сообщения (EditMessageReplyMarkup, AnswerCallbackQuery)
- `IUserService` -- нужен большинству (текущий пользователь, настройки)

### Временное состояние

Выносится в отдельный singleton-сервис `IUserSelectionState`:

```csharp
public interface IUserSelectionState
{
    ConcurrentDictionary<long, HashSet<string>> PendingCurrencies { get; }
    ConcurrentDictionary<long, HashSet<string>> PendingNewsSchedule { get; }
    ConcurrentDictionary<long, HashSet<string>> PendingCryptoCoins { get; }
}
```

**Обоснование**: состояние должно переживать Scoped-lifetime сервисов (оно static в текущей реализации). Singleton-сервис -- явная замена static-полям, легче тестировать (можно подменить через DI).

### Reply-клавиатура (`GetMainKeyboard`)

Остается статическим методом в `CommandService` (или выносится в `KeyboardFactory`). Клавиатура не содержит бизнес-логики -- это pure function от констант `BotPhrases`.

### DI-регистрация

Handler'ы регистрируются как Scoped-сервисы (аналогично другим сервисам проекта). `CommandService` получает их через конструктор:

```csharp
// Startup.cs
services.AddSingleton<IUserSelectionState, UserSelectionState>();
services.AddScoped<StartHandler>();
services.AddScoped<ValuteHandler>();
services.AddScoped<CurrenciesHandler>();
services.AddScoped<StatisticsHandler>();
services.AddScoped<SubscriptionHandler>();
services.AddScoped<NewsHandler>();
services.AddScoped<CryptoHandler>();
services.AddScoped<ICommandBot, CommandService>();
```

Handler'ы -- **конкретные классы без интерфейсов**. Интерфейсы не нужны: handler'ы не взаимозаменяемы, мокать их для unit-тестов `CommandService` не имеет смысла (роутер тривиален).

### Интерфейс ICommandBot

**Не меняется**. Единственный метод `SetCommandBot(Update)` -- это контракт для `UpdateController` и `PollingBackgroundService`. Рефакторинг внутренний, внешний API стабилен.

### Расположение файлов

```
src/bot/ExchangeRatesBot.App/
  Handlers/                        # НОВАЯ папка
    StartHandler.cs
    ValuteHandler.cs
    CurrenciesHandler.cs
    StatisticsHandler.cs
    SubscriptionHandler.cs
    NewsHandler.cs
    CryptoHandler.cs
  Services/
    CommandService.cs              # Тонкий роутер (100-120 строк)
    UserSelectionState.cs          # Singleton с ConcurrentDictionary
    ...остальные без изменений
```

## Альтернативы

### 1. Автоматическая маршрутизация через атрибуты

Каждый handler помечается атрибутом `[BotCommand("/start")]` или `[CallbackPrefix("crypto_")]`, а роутер сканирует их через рефлексию.

**Отклонено**: overengineering для 7 handler'ов. Рефлексия усложняет отладку, не дает преимуществ при таком количестве маршрутов. Можно вернуться если handler'ов станет 20+.

### 2. Один handler на каждый callback

Вместо `CryptoHandler` (обрабатывающий `crypto_rub`, `crypto_usd`, `crypto_refresh_*`) -- отдельный класс на каждый callback.

**Отклонено**: слишком мелкая гранулярность. Callback'и одного домена тесно связаны (общее состояние, общие утилиты). 30+ классов по 20-40 строк хуже 7 классов по 80-120 строк.

### 3. Оставить как есть, разбить только визуально (partial class)

`CommandService` как partial class в нескольких файлах.

**Отклонено**: partial class не решает проблему зависимостей (конструктор остается с 7 параметрами), не улучшает тестируемость, маскирует проблему вместо решения.

## Последствия

### Положительные

- **SRP**: каждый handler отвечает за один домен (80-120 строк вместо 810)
- **Навигация**: открыл `CryptoHandler.cs` -- видишь все про крипто
- **Конфликты**: фичи в разных доменах не конфликтуют при мерже
- **Тестируемость**: handler можно протестировать с минимальным набором моков
- **Зависимости**: каждый handler запрашивает только то, что использует
- **Онбординг**: новый разработчик быстрее ориентируется в 7 файлах по 100 строк

### Отрицательные

- **Количество файлов**: +9 новых файлов (7 handler'ов + UserSelectionState + IUserSelectionState)
- **Indirection**: чтобы понять полный поток обработки, нужно открыть CommandService + конкретный handler
- **DI**: CommandService получает 7 handler'ов через конструктор (вместо 7 сервисов -- та же цифра, но другой уровень абстракции)

### Риски

- **Регрессия маршрутизации**: при переносе callback-matching из if/switch в handler'ы можно потерять маршрут. **Митигация**: ручное тестирование каждого callback после рефакторинга
- **Static state race conditions**: вынос `ConcurrentDictionary` в singleton не меняет семантику, но делает зависимость явной. **Митигация**: точное воспроизведение текущего поведения, без оптимизаций state management в рамках этой таски
