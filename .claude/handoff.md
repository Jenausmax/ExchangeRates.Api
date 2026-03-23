# Handoff — Журнал передачи дел

Файл для фиксации проделанной работы, планов и блокеров между сессиями.

---

## 2026-03-23 (сессия 2)

### Сделано сегодня
- **BOT-0023: Интеграция KriptoService с Telegram-ботом** (ветка `feature/BOT-0023-kripto-bot-integration`):
  - Кнопка «Крипто» в reply-клавиатуре (3-й ряд: Новости | Крипто)
  - Команда `/crypto` — курсы топ-10 монет в RUB
  - Inline-кнопки: переключение RUB/USD + обновление данных
  - `IKriptoApiClient` + `KriptoApiClientService` — HTTP-клиент по паттерну NewsApiClientService
  - `BotConfig.KriptoServiceUrl` — URL крипто-сервиса
  - Markdown escaping для динамических данных из API
  - CultureInfo.InvariantCulture для форматирования цен
  - Docker: depends_on kripto, BotConfig__KriptoServiceUrl
  - Код-ревью: исправлены NullReferenceException в callback, Markdown injection, CultureInfo
  - Задеплоено — все 4 контейнера работают
- **Обновлён CLAUDE.md**:
  - Добавлена секция KriptoService (структура, API, конфиг)
  - Обновлены: обзор проекта (4 микросервиса), Docker таблица, команды сборки, EF миграции
  - Обновлены: inline callbacks, reply-клавиатура, команды бота, BotConfig

### Запланировано
- Тестирование BOT-0023 в Telegram
- BOT-0024: Персонализация криптовалют (UserDb.CryptoCoins, выбор монет)

### Идеи
- Подписка на крипто-рассылку (фоновая задача, уведомления по расписанию)
- Уведомления по изменению курса крипты (порог %)
- Визуальный индикатор важности новостей (огоньки по SourceCount)

### Блокеры
- Нет

---

## 2026-03-23

### Сделано сегодня
- **BOT-0021** слита в `develop`
- **BOT-0017** отменена — функциональность покрыта BOT-0021 (toggle через inline-меню подписок)
- Обновлён хук проверки git-ветки — исключения для `handoff.md` и `doc/`
- **BOT-0022: KriptoService — микросервис криптовалют** (ветка `feature/BOT-0022-kripto-service`, слита в `develop`):
  - 4-й микросервис, 7 проектов по паттерну NewsService (34 файла, +1150 строк)
  - Источник: CryptoCompare API (100K вызовов/мес, прямая поддержка RUB)
  - Топ-10 монет: BTC, ETH, SOL, XRP, BNB, USDT, DOGE, ADA, TON, AVAX
  - Фоновый фетчинг каждые 5 мин, REST API (`/api/crypto/latest`, `/history`, `/status`)
  - Docker-контейнер `exchangerates-kripto` на порту 5003
  - IHttpClientFactory, ExecuteDeleteAsync, валидация hours
  - Задеплоено — все 4 контейнера работают

### Запланировано
- Обновить CLAUDE.md (inline callbacks, команды, KriptoService)
- BOT-0023: Интеграция KriptoService с ботом (кнопка «Крипто», команда `/crypto`)

### Идеи
- Визуальный индикатор важности (огоньки по SourceCount)
- Персонализация криптовалют (UserDb.CryptoCoins, выбор монет пользователем)
- Подписка на уведомления по изменению курса крипты

### Блокеры
- Нет

---

## 2026-03-22

### Сделано сегодня
- **BOT-0019** слита в `develop`
- **BOT-0020: Проверка обновления в deploy.sh + консолидация баз данных** (ветка `feature/BOT-0020-deploy-update-check`, слита в `develop`):
  - `deploy.sh` v3.0: меню обновления/переустановки при существующем проекте (внутри — 4 варианта, рядом — 3)
  - Бэкап `.env` и баз данных при переустановке, автовосстановление
  - Автомиграция старых volumes (`data/`, `bot-data/`, `news-data/`) → `databases/{api,bot,news}-data/`
  - `docker-compose.yml`: volumes перенесены в `./databases/`
  - Данные мигрированы, контейнеры пересобраны и запущены
- **BOT-0021: Объединение подписок под одну кнопку** (ветка `feature/BOT-0021-unified-subscriptions`, в работе):
  - Кнопка «Подписка» → единое inline-меню с toggle-кнопками (✅/❌ Курсы валют, Новостной дайджест ▶, ✅/❌ Важные новости)
  - Toggle курсов и важных новостей — одним нажатием, меню обновляется через EditMessage
  - Подменю дайджеста: toggle подписки + расписание + кнопка «← Назад»
  - Кнопка «Новости» → сразу лента последних 5 новостей (без промежуточного меню)
  - Добавлены поля `Subscribe`, `NewsSubscribe`, `ImportantNewsSubscribe` в `CurrentUser`
  - Старые callback-и сохранены для обратной совместимости (редирект)
  - Сборка: 0 ошибок, 22 проекта

### Запланировано на следующую сессию
- Задеплоить BOT-0021, протестировать в Telegram
- Слить BOT-0021 в `develop`
- Обновить CLAUDE.md (inline callbacks, команды)

### Идеи
- Визуальный индикатор важности (огоньки по SourceCount)
- Проработка фичи криптовалют
- Проработать скрипт деплоя (`deploy.sh`) — реализовано в BOT-0020 ✅

### Блокеры
- Нет

---

## 2026-03-21

### Сделано сегодня
- **BOT-0019: Ротация важных новостей** (ветка `feature/BOT-0019-important-news-rotation`):
  - Добавлен `ImportantNewsMaxAgeHours = 2` в `NewsConfig` — фильтрация устаревших новостей
  - Обновлён `INewsRepository` и `NewsRepository` — параметр `maxAgeHours` с фильтром по `FetchedAt`
  - `NewsDigestService.GetMostImportantAsync` передаёт maxAge из конфига
  - `JobsSendImportantNews` теперь вызывает `MarkSentAsync` после рассылки — новость помечается отправленной
  - Создан файл фичи `doc/feature/20260321-important-news-rotation-bot-0019.md`
  - Собрано и задеплоено в Docker — все 3 контейнера работают

### Запланировано на следующую сессию
- Определяется по следующей задаче

### Идеи
- Проработать скрипт деплоя (`deploy.sh`) — добавить секцию обновления: если папка `ExchangeRates.Api` уже существует, спросить пользователя «Хотите обновить проект?». Если да — предложить выбор: pull изменений по master или удаление папки и клонирование проекта заново.

### Блокеры
- Нет

---

## 2026-03-20

### Сделано сегодня
- **Сессия 3 (вечер):**
  - Ветка `feature/BOT-0016-important-news` слита в `develop`
  - Деплой в Docker — все 3 контейнера собраны и запущены (api, bot, news)
  - **BOT-0018: Обновление deploy.sh** (ветка `feature/BOT-0018-deploy-script-update`, коммит `427c1b4`):
    - Добавлены промпты для новостного дайджеста (вкл/выкл, время)
    - Добавлены промпты для важных новостей
    - Добавлен выбор LLM-провайдера (отключён / Polza с API-ключом / Ollama с URL)
    - Генерируемый `.env` содержит все 11 переменных (как в `.env.example`)
    - Обновлено финальное сообщение с адресами всех сервисов
- **BOT-0016: Подписка на важные новости** — полная реализация фичи (см. ниже)
- **NewsService:**
  - Поле `SourceCount` в `NewsTopicDb` + индекс + EF-миграция `AddSourceCount`
  - Алгоритм Jaccard-similarity на символьных триграммах в `NewsNormalizationHelper` (3 метода + стоп-слова)
  - 3 новых метода в `NewsRepository` (`GetRecentTopicsForSimilarityAsync`, `GetMostImportantUnsentAsync`, `IncrementSourceCountAsync`)
  - Изменена дедупликация: при Jaccard >= 0.5 инкрементирует `SourceCount` вместо создания дубликата
  - Конфиг: `SimilarityThreshold = 0.5`, `FetchIntervalMinutes` 60→30
  - Метод `GetMostImportantAsync` в `NewsDigestService`
  - Эндпоинт `GET /api/digest/top` в `DigestController`
- **Bot:**
  - Поля `ImportantNewsSubscribe`, `LastImportantNewsAt` в `UserDb` + EF-миграция `AddImportantNewsSubscribe`
  - Методы `ImportantNewsSubscribeUpdate`, `UpdateLastImportantNewsAt` в `UserService`
  - Метод `GetMostImportantAsync` в `NewsApiClientService`
  - Фоновая задача `JobsSendImportantNews` (раз в час, XX:00)
  - Кнопки «Важные новости: вкл/выкл» в inline-меню новостей
  - 3 новых фразы в `BotPhrases`
  - Конфиг `ImportantNewsEnabled` + условная регистрация в `Startup`
- **Docker:** переменная `IMPORTANT_NEWS_ENABLED` в compose + .env.example
- Документация: `doc/feature/20260320-important-news-bot-0016.md`
- Сборка: 0 ошибок, 22 проекта
- **Сессия 2 (вечер):**
  - Создан план `doc/feature/20260320-important-command-bot-0017.md` — команда `/important` (toggle подписки на важные новости)
  - Закоммичено и запушено всё по BOT-0016 + план BOT-0017 (коммит `b7cde05`)
  - Добавлены `.agents/` и `skills-lock.json` в `.gitignore` (коммит `41a8fcc`)
  - Ветка `feature/BOT-0016-important-news` запушена в remote

### Запланировано на следующую сессию
- Слить `feature/BOT-0018-deploy-script-update` в `develop`
- Тестирование BOT-0016 в Docker (функциональное)
- Проверить что при повторном RSS-парсинге `SourceCount` инкрементируется для похожих новостей
- Проверить `curl GET /api/digest/top` — одна новость с максимальным SourceCount
- Протестировать в боте: Новости → «Важные новости: вкл» → через час приходит новость
- Возможно потребуется подобрать порог `SimilarityThreshold` (сейчас 0.5)
- Реализация BOT-0017 (`/important` команда)

### Идеи
- Подумать о визуальном индикаторе важности (например, кол-во огоньков по SourceCount)
- Проработать фичу по добавлению криптовалют

### Блокеры
- (нет)

---

## 2026-03-19

### Сделано сегодня
- Создан файл `.claude/handoff.md` для ведения журнала передачи дел
- Добавлено правило в `CLAUDE.md` об обязательном ведении handoff

### Запланировано на завтра
- (пока нет)

### Идеи
- (пока нет)

### Блокеры
- (пока нет)
