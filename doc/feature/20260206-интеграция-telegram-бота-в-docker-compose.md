- [x] Реализовано

# План интеграции Telegram-бота в Docker Compose

## Обзор

Интегрировать Telegram-бота (ExchangeRatesBot) в существующий docker-compose.yml для работы совместно с API (ExchangeRates.Api). Бот будет получать данные курсов валют от API через внутреннюю Docker сеть.

## Текущая архитектура

**API (ExchangeRates.Api):**
- Контроллер: `ValuteController` на корневом пути "/"
- Эндпоинт: `POST /?charcode={code}&day={days}`
- Docker: Уже настроен в docker-compose.yml
- БД: SQLite (Data.db) с курсами валют

**Telegram Bot (ExchangeRatesBot):**
- Использует `BotConfig.UrlRequest` для подключения к API
- Делает POST запросы: `POST /?charcode={code}&day={day}`
- Фоновая задача `JobsSendMessageUsers` (каждую минуту) для рассылки
- БД: Отдельная SQLite (Data.db) с пользователями Telegram
- Dockerfile: `src/bot/ExchangeRatesBot/Dockerfile` (multi-stage build)

## Ключевые файлы для изменения

1. **docker-compose.yml** (корень проекта) - добавить сервис бота
2. **.env** (корень проекта, создать новый) - токен бота и другие секреты
3. **.gitignore** (корень проекта) - добавить .env для защиты секретов

**НЕ требуется изменение:**
- `src/bot/ExchangeRatesBot/Dockerfile` - корректен (ExchangeRates.Api.sln включает все проекты бота)

## План реализации

### Шаг 1: Создать файл .env для секретов

Создать файл `.env` в корне проекта со следующим содержимым:

```bash
# Токен Telegram бота (получить от @BotFather)
# ВАЖНО: Заменить на реальный токен перед запуском!
BOT_TOKEN=718470687:AAF-SsRrPbXWoPyHLo8lIN7aHowpGzjg-Go

# Webhook URL (оставить пустым для режима polling)
BOT_WEBHOOK=

# Время рассылки сообщений подписчикам
BOT_TIME_ONE=14:05
BOT_TIME_TWO=15:32
```

**Важно:** Этот файл содержит секретные данные и НЕ должен попадать в git!

### Шаг 2: Обновить .gitignore

Добавить в `.gitignore` (если файл не существует - создать):

```
.env
```

### Шаг 3: Добавить сервис бота в docker-compose.yml

Добавить следующий сервис в секцию `services:` файла `docker-compose.yml`:

```yaml
  exchangerates-bot:
    build:
      context: ./src
      dockerfile: bot/ExchangeRatesBot/Dockerfile
    container_name: exchangerates-bot
    depends_on:
      - exchangerates-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      # КРИТИЧНО: Внутренний адрес API в Docker сети
      - BotConfig__UrlRequest=http://exchangerates-api:80/
      # Токен бота из .env файла
      - BotConfig__BotToken=${BOT_TOKEN}
      # Webhook (пустой = режим polling, рекомендуется для разработки)
      - BotConfig__Webhook=${BOT_WEBHOOK:-}
      # Время рассылки сообщений
      - BotConfig__TimeOne=${BOT_TIME_ONE:-14:05}
      - BotConfig__TimeTwo=${BOT_TIME_TWO:-15:32}
      # ОТДЕЛЬНАЯ БД бота (не путать с БД API)
      - ConnectionStrings__SqliteConnection=Data Source=/app/data/Data.db;foreign keys=false;
    volumes:
      # ОТДЕЛЬНЫЕ volumes от API
      - ./bot-data:/app/data
      - ./bot-logs:/app/logs
    restart: unless-stopped
    networks:
      - exchangerates-network
```

**Ключевые моменты:**
- `context: ./src` - build context совместим с Dockerfile бота
- `dockerfile: bot/ExchangeRatesBot/Dockerfile` - путь относительно context
- `depends_on: exchangerates-api` - гарантирует запуск API первым
- `BotConfig__UrlRequest=http://exchangerates-api:80/` - внутренний DNS Docker для связи с API
- Volumes `./bot-data` и `./bot-logs` отделены от API (`./data` и `./logs`)
- Обе сети используют `exchangerates-network`

### Шаг 4: (Опционально) Добавить healthcheck для API

Для более надежного запуска можно добавить healthcheck в сервис `exchangerates-api`:

```yaml
  exchangerates-api:
    # ... существующие настройки ...
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

И изменить `depends_on` в боте:

```yaml
  exchangerates-bot:
    depends_on:
      exchangerates-api:
        condition: service_healthy
```

**Примечание:** Если используется Docker Compose v2, `condition` может не поддерживаться. В этом случае оставить просто:
```yaml
    depends_on:
      - exchangerates-api
```

## Структура итоговых файлов

```
ExchangeRates.Api/
├── .env                          # Секреты (НЕ коммитить!)
├── .gitignore                    # Защита .env
├── docker-compose.yml            # Обновленная конфигурация (2 сервиса)
├── data/                         # API database (создастся автоматически)
├── logs/                         # API logs (создастся автоматически)
├── bot-data/                     # Bot database (создастся автоматически)
├── bot-logs/                     # Bot logs (создастся автоматически)
└── src/
    ├── ExchangeRates.Api.sln     # Включает ВСЕ проекты (API + Bot)
    ├── ExchangeRates.Api/
    │   └── Dockerfile
    └── bot/
        └── ExchangeRatesBot/
            └── Dockerfile
```

## Команды для запуска

### Первый запуск

```bash
# 1. Перейти в корень проекта
cd C:\Users\mminm\Documents\ExchangeRates.Api

# 2. Создать .env файл (см. Шаг 1)
# ВАЖНО: Заменить BOT_TOKEN на реальный токен от @BotFather!

# 3. Сборка образов
docker-compose build

# 4. Запуск всех сервисов в фоновом режиме
docker-compose up -d

# 5. Просмотр логов
docker-compose logs -f
```

### Проверка работоспособности

```bash
# Проверить статус контейнеров
docker-compose ps

# Просмотреть логи бота
docker-compose logs -f exchangerates-bot

# Просмотреть логи API
docker-compose logs -f exchangerates-api

# Проверить что бот может обратиться к API
docker exec -it exchangerates-bot curl http://exchangerates-api:80/

# Проверить созданные БД
ls -la data/Data.db       # API database
ls -la bot-data/Data.db   # Bot database
```

### Управление контейнерами

```bash
# Остановить все сервисы
docker-compose down

# Пересборка и перезапуск
docker-compose up -d --build

# Перезапуск только бота
docker-compose restart exchangerates-bot

# Просмотр логов с фильтрацией
docker-compose logs exchangerates-bot | grep -i "error\|exception"
```

## Проверка интеграции

### Тест 1: Проверить сетевое взаимодействие

```bash
# Из контейнера бота проверить доступность API
docker exec -it exchangerates-bot curl -v http://exchangerates-api:80/?charcode=USD&day=7
```

Ожидаемый результат: JSON с курсами USD за 7 дней.

### Тест 2: Проверить логи бота на ошибки десериализации

```bash
docker-compose logs exchangerates-bot | grep -i "deserialize"
```

Ожидаемый результат: "Deserialize success" (если бот делал запросы к API).

### Тест 3: Проверить Telegram бота

1. Найти бота в Telegram (использовать токен из .env)
2. Отправить команду `/start`
3. Отправить команду `/valuteoneday` или `/valutesevendays`
4. Проверить что бот отвечает с данными курсов

### Тест 4: Проверить фоновую задачу

```bash
# Просмотреть логи бота в реальном времени
docker-compose logs -f exchangerates-bot

# Дождаться времени TimeOne (14:05) или TimeTwo (15:32)
# В логах должна появиться информация о рассылке сообщений
```

## Возможные проблемы и решения

### Проблема 1: Бот не может подключиться к API

**Симптомы:**
```
Error: Deserialize ProcessingService
Connection refused
```

**Решения:**
1. Проверить что `BotConfig__UrlRequest=http://exchangerates-api:80/` (с `/` в конце)
2. Убедиться что оба сервиса в одной сети: `docker network inspect exchangerates_exchangerates-network`
3. Проверить что API запущен: `docker-compose ps`
4. Проверить логи API на ошибки: `docker-compose logs exchangerates-api`

### Проблема 2: Токен бота не работает

**Симптомы:**
```
Unauthorized
Invalid token
```

**Решения:**
1. Проверить что токен в .env файле корректный
2. Получить новый токен от @BotFather в Telegram
3. Перезапустить контейнер: `docker-compose restart exchangerates-bot`

### Проблема 3: Volumes не создаются

**Симптомы:**
```
Database not found
Directory not found
```

**Решения:**
1. Вручную создать папки:
   ```bash
   mkdir -p bot-data bot-logs
   ```
2. Проверить права доступа к папкам
3. Перезапустить контейнеры: `docker-compose up -d --force-recreate`

### Проблема 4: Миграции БД не применились

**Симптомы:**
```
Table 'Users' not found
```

**Решения:**
1. Проверить логи на этапе запуска контейнера
2. Возможно автоматическая миграция в Program.cs бота не настроена
3. Применить миграции вручную:
   ```bash
   docker exec -it exchangerates-bot dotnet ef database update
   ```

## Рекомендации по безопасности

1. **Токен бота:**
   - НИКОГДА не коммитить .env в git
   - Использовать разные токены для dev/prod
   - Периодически обновлять токен

2. **Webhook:**
   - Для production настроить HTTPS через reverse proxy (nginx/traefik)
   - Использовать Let's Encrypt для SSL сертификатов
   - Для разработки использовать polling (BotConfig__Webhook пустой)

3. **Volumes:**
   - Регулярно делать бэкапы БД:
     ```bash
     cp bot-data/Data.db bot-data/Data.db.backup
     ```
   - Настроить ротацию логов для предотвращения переполнения диска

4. **Сеть:**
   - Не открывать порты бота наружу без необходимости
   - Вся коммуникация между ботом и API идет через внутреннюю сеть

## Проверочный чеклист перед запуском

- [ ] Создан файл `.env` с реальным токеном бота
- [ ] `.env` добавлен в `.gitignore`
- [ ] Обновлен `docker-compose.yml` с сервисом `exchangerates-bot`
- [ ] Проверено что `BotConfig__UrlRequest=http://exchangerates-api:80/`
- [ ] Volumes для бота отделены от API (`./bot-data`, `./bot-logs`)
- [ ] Настроен `depends_on` для правильной последовательности запуска
- [ ] Токен бота получен от @BotFather в Telegram
- [ ] Решено нужен ли webhook (рекомендуется оставить пустым для dev)

## Критические файлы

- `C:\Users\mminm\Documents\ExchangeRates.Api\docker-compose.yml` - Главный файл конфигурации
- `C:\Users\mminm\Documents\ExchangeRates.Api\.env` - Секретные данные (создать новый)
- `C:\Users\mminm\Documents\ExchangeRates.Api\.gitignore` - Защита секретов
- `C:\Users\mminm\Documents\ExchangeRates.Api\src\bot\ExchangeRatesBot\Dockerfile` - Dockerfile бота (НЕ изменять, он корректен)
- `C:\Users\mminm\Documents\ExchangeRates.Api\src\ExchangeRates.Api.sln` - Solution включает все проекты (API + Bot)

## Итоговые изменения

| Файл | Действие | Описание |
|------|----------|----------|
| `.env` | Создать | Токен бота и другие секреты |
| `.gitignore` | Обновить/Создать | Добавить `.env` |
| `docker-compose.yml` | Обновить | Добавить сервис `exchangerates-bot` |
| Dockerfile бота | Без изменений | Уже корректен |

## Поэтапное внедрение

**Этап 1: Подготовка (5 минут)**
1. Создать `.env` файл с токеном
2. Обновить `.gitignore`
3. Обновить `docker-compose.yml`

**Этап 2: Сборка (10-15 минут)**
1. `docker-compose build exchangerates-bot`
2. Проверить логи сборки на ошибки

**Этап 3: Запуск и тестирование (5 минут)**
1. `docker-compose up -d`
2. Проверить логи: `docker-compose logs -f`
3. Тест связи: `docker exec exchangerates-bot curl http://exchangerates-api:80/`

**Этап 4: Проверка функциональности (10 минут)**
1. Отправить команду боту в Telegram
2. Проверить что бот получает данные от API
3. Проверить работу фоновой задачи

**Общее время: ~30-35 минут**

## Дополнительные возможности (опционально)

### Настройка webhook через ngrok

Для тестирования webhook в локальной разработке:

```bash
# 1. Установить ngrok
# 2. Запустить туннель
ngrok http 5002

# 3. Обновить .env
BOT_WEBHOOK=https://your-ngrok-url.ngrok.io

# 4. Раскомментировать порты в docker-compose.yml
ports:
  - "5002:80"
  - "5003:443"

# 5. Перезапустить бота
docker-compose restart exchangerates-bot
```

### Мониторинг логов в реальном времени

```bash
# Все сервисы с фильтрацией
docker-compose logs -f | grep -i "error\|warning\|exception"

# Только критические ошибки
docker-compose logs -f exchangerates-bot | grep -i "error"

# Последние 100 строк
docker-compose logs --tail=100 -f exchangerates-bot
```

### Backup скрипт для БД

Создать `backup.sh` в корне проекта:

```bash
#!/bin/bash
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p backups
cp data/Data.db backups/api_${DATE}.db
cp bot-data/Data.db backups/bot_${DATE}.db
echo "Backup created: backups/*_${DATE}.db"
```

Запуск: `bash backup.sh`

---

## Финальная конфигурация docker-compose.yml

Полная версия файла после всех изменений:

```yaml
version: '3.8'

services:
  exchangerates-api:
    build:
      context: ./src
      dockerfile: ExchangeRates.Api/Dockerfile
    container_name: exchangerates-api
    ports:
      - "5000:80"
      - "5001:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - ConnectionStrings__DbData=Data Source=/app/data/Data.db;foreign keys=false;
      - ClientConfig__SiteApi=https://www.cbr-xml-daily.ru/
      - ClientConfig__SiteGet=daily_json.js
      - ClientConfig__PeriodMinute=30
      - ClientConfig__TimeUpdateJobs=08:40
      - ClientConfig__JobsValute=false
      - ClientConfig__JobsValuteToHour=true
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
    restart: unless-stopped
    networks:
      - exchangerates-network

  exchangerates-bot:
    build:
      context: ./src
      dockerfile: bot/ExchangeRatesBot/Dockerfile
    container_name: exchangerates-bot
    depends_on:
      - exchangerates-api
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
      - BotConfig__UrlRequest=http://exchangerates-api:80/
      - BotConfig__BotToken=${BOT_TOKEN}
      - BotConfig__Webhook=${BOT_WEBHOOK:-}
      - BotConfig__TimeOne=${BOT_TIME_ONE:-14:05}
      - BotConfig__TimeTwo=${BOT_TIME_TWO:-15:32}
      - ConnectionStrings__SqliteConnection=Data Source=/app/data/Data.db;foreign keys=false;
    volumes:
      - ./bot-data:/app/data
      - ./bot-logs:/app/logs
    restart: unless-stopped
    networks:
      - exchangerates-network

networks:
  exchangerates-network:
    driver: bridge
```
