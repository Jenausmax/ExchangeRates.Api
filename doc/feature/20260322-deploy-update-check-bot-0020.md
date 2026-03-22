# BOT-0020: Проверка обновления в deploy.sh + консолидация баз данных

- [ ] Не реализовано

## Контекст

Скрипт `deploy.sh` при повторном запуске (папка `ExchangeRates.Api` уже существует) не даёт выбора «удалить и склонировать заново». Также базы данных разбросаны по трём отдельным папкам (`./data`, `./bot-data`, `./news-data`), что неудобно для бэкапов.

Цель:
1. Явный шаг обновления с выбором: pull / переустановка / отмена
2. При переустановке — бэкап `.env` и баз данных
3. Консолидация всех БД в единую папку `./databases/`

## Часть 1: Консолидация баз данных

### Текущее состояние volumes (docker-compose.yml)

| Сервис | Host volume | Контейнер | Файл БД |
|--------|------------|-----------|---------|
| API | `./data` | `/app/data` | `Data.db`, `log.db` |
| Bot | `./bot-data` | `/app/data` | `Data.db`, `log.db` |
| News | `./news-data` | `/app/data` | `News.db`, `log.db` |

### Новая структура

Все данные — в единой папке `./databases/` с подпапками по проектам:

```
./databases/
  api-data/       ← Data.db, log.db (API)
  bot-data/       ← Data.db, log.db (бот)
  news-data/      ← News.db, log.db (новости)
```

**Изменения в `docker-compose.yml`:**

```yaml
# API
volumes:
  - ./databases/api-data:/app/data    # было: ./data:/app/data
  - ./logs:/app/logs

# Bot
volumes:
  - ./databases/bot-data:/app/data    # было: ./bot-data:/app/data
  - ./bot-logs:/app/logs

# News
volumes:
  - ./databases/news-data:/app/data   # было: ./news-data:/app/data
  - ./news-logs:/app/logs
```

**Миграция данных в deploy.sh:** Если при обновлении обнаружены старые папки (`./data`, `./bot-data`, `./news-data`) — предложить автоматически переместить их в `./databases/`.

## Часть 2: Шаг обновления в deploy.sh

### Сценарий А: запуск **рядом** с папкой `ExchangeRates.Api`

Вместо текущих 2 вариантов (использовать / отменить) — 3 варианта:

```
  Папка 'ExchangeRates.Api' уже существует. Что вы хотите сделать?
    1) Обновить — pull изменений по выбранной ветке
    2) Переустановить — удалить и склонировать заново
    3) Отменить
```

### Сценарий Б: запуск **внутри** проекта

```
  Обнаружен существующий проект. Что вы хотите сделать?
    1) Обновить — pull изменений по выбранной ветке
    2) Переустановить — удалить и склонировать заново
    3) Продолжить без обновления (только пересобрать Docker)
    4) Отменить
```

### Логика варианта «Переустановить»

1. Предупреждение: «Папка будет удалена! Данные будут сохранены в бэкап.»
2. Подтверждение через `prompt_yes_no`
3. Бэкап в папку `./backup_YYYYMMDD_HHMMSS/`:
   - `.env` → `backup/env.bak`
   - `./databases/` (или старые `./data`, `./bot-data`, `./news-data`) → `backup/databases/`
4. Удаление проекта: `rm -rf ExchangeRates.Api` (или очистка если внутри)
5. Клонирование: `git clone -b $BRANCH ...`
6. Восстановление `.env` и `./databases/` из бэкапа
7. Сообщение: «Бэкап сохранён в ./backup_.../, базы данных и .env восстановлены»

### Новая функция `backup_project_data()`

```bash
backup_project_data() {
    local backup_dir="backup_$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$backup_dir"

    # Бэкап .env
    if [ -f "$ENV_FILE" ]; then
        cp "$ENV_FILE" "$backup_dir/env.bak"
        print_success "Бэкап .env создан"
    fi

    # Бэкап баз данных (новая структура)
    if [ -d "databases" ]; then
        cp -r databases "$backup_dir/databases"
        print_success "Бэкап баз данных создан"
    else
        # Миграция со старой структуры
        mkdir -p "$backup_dir/databases"
        [ -d "data" ] && cp -r data "$backup_dir/databases/api-data"
        [ -d "bot-data" ] && cp -r bot-data "$backup_dir/databases/bot-data"
        [ -d "news-data" ] && cp -r news-data "$backup_dir/databases/news-data"
        print_success "Бэкап баз данных (старый формат) создан"
    fi

    echo "$backup_dir"
}
```

### Новая функция `restore_project_data()`

```bash
restore_project_data() {
    local backup_dir="$1"

    if [ -f "$backup_dir/env.bak" ]; then
        cp "$backup_dir/env.bak" "$ENV_FILE"
        print_success ".env восстановлен из бэкапа"
    fi

    if [ -d "$backup_dir/databases" ]; then
        cp -r "$backup_dir/databases" databases
        print_success "Базы данных восстановлены из бэкапа"
    fi
}
```

### Новая функция `migrate_old_volumes()`

При обновлении (не переустановке) — автоматическая миграция старых папок в новую структуру:

```bash
migrate_old_volumes() {
    if [ -d "data" ] && [ ! -d "databases/api-data" ]; then
        print_info "Миграция баз данных в новую структуру..."
        mkdir -p databases
        mv data databases/api-data
        mv bot-data databases/bot-data 2>/dev/null
        mv news-data databases/news-data 2>/dev/null
        print_success "Миграция завершена: ./databases/"
    fi
}
```

## Файлы для изменения

1. **`deploy.sh`** — рефакторинг `clone_or_update_repo()`, новые функции бэкапа/восстановления/миграции
2. **`docker-compose.yml`** — volumes: `./databases/api-data`, `./databases/bot-data`, `./databases/news-data`

## Проверка

1. **Новая установка:** `deploy.sh` в пустой папке — клонирование, создание `./databases/` с подпапками
2. **Обновление (рядом):** папка `ExchangeRates.Api` есть → 3 варианта, pull работает
3. **Обновление (внутри):** запуск внутри проекта → 4 варианта
4. **Переустановка:** бэкап создаётся, проект удаляется, клонируется заново, `.env` и базы восстановлены
5. **Миграция:** старые папки `./data`, `./bot-data`, `./news-data` → перемещаются в `./databases/`
6. **Docker:** `docker-compose up -d` — контейнеры стартуют, базы доступны по новым путям
