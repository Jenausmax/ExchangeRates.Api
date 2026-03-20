#!/bin/bash

#==================================================================================================
#  ExchangeRates.Api - Скрипт автоматизированного развертывания
#
#  Описание: Полная автоматизация клонирования, сборки и запуска проекта ExchangeRates.Api
#  Требования: Bash 4.0+, Git, Docker 20.10+, Docker Compose 1.29+
#
#  Автор: Software Architect Agent
#  Версия: 2.0
#  Дата: 2026-03-20
#==================================================================================================

#--------------------------------------------------------------------------------------------------
#  Проверка интерактивного режима
#--------------------------------------------------------------------------------------------------

# Проверяем, запущен ли скрипт интерактивно
if [ ! -t 0 ]; then
    echo -e "\033[1;31mОшибка: Скрипт запущен в неинтерактивном режиме (через pipe).\033[0m"
    echo ""
    echo "Этот скрипт требует интерактивный ввод пользователя."
    echo "Пожалуйста, выполните следующие шаги:"
    echo ""
    echo "  1. Скачайте скрипт:"
    echo "     curl -fsSL https://raw.githubusercontent.com/Jenausmax/ExchangeRates.Api/main/deploy.sh -o deploy.sh"
    echo ""
    echo "  2. Сделайте скрипт исполняемым:"
    echo "     chmod +x deploy.sh"
    echo ""
    echo "  3. Запустите скрипт:"
    echo "     ./deploy.sh"
    echo ""
    echo "Или для Windows через Git Bash:"
    echo "  curl -fsSL https://raw.githubusercontent.com/Jenausmax/ExchangeRates.Api/main/deploy.sh -o deploy.sh"
    echo "  bash deploy.sh"
    echo ""
    exit 1
fi

#--------------------------------------------------------------------------------------------------
#  Константы проекта
#--------------------------------------------------------------------------------------------------
readonly PROJECT_NAME="ExchangeRates.Api"
readonly GITHUB_REPO="https://github.com/Jenausmax/ExchangeRates.Api.git"
readonly DEFAULT_BRANCH="master"
readonly ENV_FILE=".env"
readonly COMPOSE_FILE="docker-compose.yml"

#--------------------------------------------------------------------------------------------------
#  Цвета для вывода (ANSI escape codes)
#--------------------------------------------------------------------------------------------------
readonly COLOR_RED='\033[0;31m'
readonly COLOR_GREEN='\033[0;32m'
readonly COLOR_YELLOW='\033[1;33m'
readonly COLOR_BLUE='\033[0;34m'
readonly COLOR_CYAN='\033[0;36m'
readonly COLOR_WHITE='\033[1;37m'
readonly COLOR_RESET='\033[0m'

#--------------------------------------------------------------------------------------------------
#  Иконки для визуализации
#--------------------------------------------------------------------------------------------------
readonly ICON_SUCCESS="✓"
readonly ICON_ERROR="✗"
readonly ICON_WARNING="!"
readonly ICON_INFO="→"
readonly ICON_ARROW="→"

#--------------------------------------------------------------------------------------------------
#  Дефолтные значения
#--------------------------------------------------------------------------------------------------
readonly DEFAULT_TIME_ONE="14:05"
readonly DEFAULT_TIME_TWO="15:32"
readonly DEFAULT_MODE="polling"
readonly DEFAULT_NEWS_TIME="09:00"
readonly DEFAULT_OLLAMA_URL="http://localhost:11434"

#--------------------------------------------------------------------------------------------------
#  Глобальные переменные
#--------------------------------------------------------------------------------------------------
BOT_TOKEN=""
BOT_MODE=""
BOT_WEBHOOK=""
TIME_ONE=""
TIME_TWO=""
BRANCH=""
NEWS_ENABLED="false"
NEWS_TIME=""
IMPORTANT_NEWS_ENABLED="false"
LLM_PROVIDER=""
POLZA_API_KEY=""
OLLAMA_URL=""

#==================================================================================================
#  Функции логирования
#==================================================================================================

print_header() {
    echo -e "${COLOR_BLUE}========================================================================${COLOR_RESET}"
    echo -e "${COLOR_BLUE}                    ${PROJECT_NAME} - Развертывание${COLOR_RESET}"
    echo -e "${COLOR_BLUE}========================================================================${COLOR_RESET}"
    echo ""
}

print_success() {
    echo -e "${COLOR_GREEN}${ICON_SUCCESS} $1${COLOR_RESET}"
}

print_error() {
    echo -e "${COLOR_RED}${ICON_ERROR} $1${COLOR_RESET}"
}

print_info() {
    echo -e "${COLOR_WHITE}${ICON_INFO} $1${COLOR_RESET}"
}

print_warning() {
    echo -e "${COLOR_YELLOW}${ICON_WARNING} $1${COLOR_RESET}"
}

print_step() {
    echo -e "${COLOR_CYAN}[$1]$2${COLOR_RESET}"
}

print_divider() {
    echo -e "${COLOR_BLUE}========================================================================${COLOR_RESET}"
}

#==================================================================================================
#  Функции проверки зависимостей
#==================================================================================================

check_dependencies() {
    print_step "1/7" " Проверка зависимостей..."

    local all_found=true

    # Проверка Git
    if command -v git >/dev/null 2>&1; then
        local git_version=$(git --version | awk '{print $3}')
        print_success "Git найден: ${git_version}"
    else
        print_error "Git не установлен"
        print_info "Установите Git: https://git-scm.com/downloads"
        all_found=false
    fi

    # Проверка Docker
    if command -v docker >/dev/null 2>&1; then
        local docker_version=$(docker --version | awk '{print $3}' | tr -d ',')
        print_success "Docker найден: ${docker_version}"
    else
        print_error "Docker не установлен"
        print_info "Установите Docker: https://docs.docker.com/get-docker/"
        all_found=false
    fi

    # Проверка Docker Compose
    if command -v docker-compose >/dev/null 2>&1; then
        local compose_version=$(docker-compose --version | awk '{print $4}' | tr -d ',')
        print_success "Docker Compose найден: ${compose_version}"
    else
        print_error "Docker Compose не установлен"
        print_info "Установите Docker Compose: https://docs.docker.com/compose/install/"
        all_found=false
    fi

    # Проверка Docker daemon
    if ! docker info >/dev/null 2>&1; then
        print_error "Docker daemon не запущен или нет прав доступа"
        print_info "Запустите Docker Desktop или выполните:"
        print_info "  sudo systemctl start docker  (Linux)"
        print_info "  sudo usermod -aG docker \$USER  (Linux, добавить пользователя в группу)"
        all_found=false
    else
        print_success "Docker daemon запущен"
    fi

    echo ""

    if [ "$all_found" = false ]; then
        print_error "Не все зависимости установлены. Установите необходимые инструменты и повторите попытку."
        return 1
    fi

    return 0
}

check_os() {
    print_step "2/7" " Определение операционной системы..."

    local os_name
    local os_version

    # Определяем тип ОС
    case "$(uname -s)" in
        Linux*)
            os_name="Linux"
            if [ -f /etc/os-release ]; then
                os_version=$(grep '^PRETTY_NAME=' /etc/os-release | cut -d'"' -f2)
            else
                os_version=$(uname -r)
            fi
            ;;
        Darwin*)
            os_name="macOS"
            os_version=$(sw_vers -productVersion)
            ;;
        MINGW*|MSYS*|CYGWIN*)
            os_name="Windows (Git Bash/WSL)"
            os_version=$(uname -r)
            ;;
        *)
            os_name="Unknown"
            os_version=$(uname -s)
            ;;
    esac

    print_success "ОС: $os_name $os_version"
    return 0
}

#==================================================================================================
#  Функции валидации
#==================================================================================================

validate_bot_token() {
    local token="$1"

    # Проверка формата с regex: 9-10 цифр : 35 символов (буквы, цифры, _, -)
    if [[ ! "$token" =~ ^[0-9]{9,10}:[A-Za-z0-9_-]{35}$ ]]; then
        return 1
    fi

    return 0
}

#==================================================================================================
#  Функции интерактивного ввода
#==================================================================================================

prompt_yes_no() {
    local prompt="$1"
    local default="${2:-N}"

    while true; do
        read -p "  ${prompt} [${default}]: " response
        response=${response:-$default}

        case $response in
            [Yy]* ) return 0 ;;
            [Nn]* ) return 1 ;;
            * ) print_warning "Пожалуйста, введите Y (да) или N (нет)" ;;
        esac
    done
}

prompt_bot_token() {
    print_info "Для работы бота необходим токен Telegram."
    print_info "Получите токен у @BotFather в Telegram."
    echo ""

    local attempts=0
    local max_attempts=3

    while [ $attempts -lt $max_attempts ]; do
        echo -n "  Введите токен бота: "

        # Пытаемся использовать скрытый ввод, если терминал поддерживает
        if [ -t 0 ]; then
            # Пробуем прочитать токен с /dev/tty для корректной работы в разных терминалах
            read bot_token < /dev/tty
        else
            read bot_token
        fi

        # Проверяем, что токен не пустой
        if [ -z "$bot_token" ]; then
            echo ""
            print_warning "Токен не может быть пустым. Попробуйте еще раз."
            attempts=$((attempts + 1))
            continue
        fi

        echo ""

        if validate_bot_token "$bot_token"; then
            local masked_token=$(echo "$bot_token" | sed 's/./x/g')
            print_success "Токен принят: ${masked_token}"
            BOT_TOKEN="$bot_token"
            return 0
        else
            attempts=$((attempts + 1))
            print_warning "Некорректный формат токена. Попробуйте еще раз."
            print_info "Формат: 1234567890:ABCdefGHIjklMNOpqrSTUvwxYZ"
        fi
    done

    print_error "Превышено количество попыток ввода токена"
    return 1
}

prompt_bot_mode() {
    echo ""
    print_info "Выберите режим работы бота:"
    echo "    1) Polling (рекомендуется для Docker)"
    echo "    2) Webhook (требует публичный HTTPS URL)"
    echo ""

    while true; do
        read -p "  Ваш выбор [1]: " mode
        mode=${mode:-1}

        case $mode in
            1)
                BOT_MODE="polling"
                print_success "Выбран режим: Polling"
                return 0
                ;;
            2)
                BOT_MODE="webhook"
                print_success "Выбран режим: Webhook"
                return 0
                ;;
            *)
                print_warning "Пожалуйста, введите 1 или 2"
                ;;
        esac
    done
}

prompt_webhook_url() {
    echo ""
    print_info "Введите Webhook URL (полный URL с протоколом https://)"
    echo ""

    while true; do
        read -p "  Webhook URL: " webhook_url

        if [[ "$webhook_url" =~ ^https?:// ]]; then
            BOT_WEBHOOK="$webhook_url"
            print_success "Webhook URL: ${BOT_WEBHOOK}"
            return 0
        else
            print_warning "Некорректный URL. URL должен начинаться с http:// или https://"
        fi
    done
}

prompt_time() {
    echo ""
    print_info "Настройка времени рассылки (или нажмите Enter для дефолтных значений)"

    read -p "  Время первой рассылки [${DEFAULT_TIME_ONE}]: " time_one
    TIME_ONE="${time_one:-$DEFAULT_TIME_ONE}"

    read -p "  Время второй рассылки [${DEFAULT_TIME_TWO}]: " time_two
    TIME_TWO="${time_two:-$DEFAULT_TIME_TWO}"

    print_success "Время рассылки настроено: ${TIME_ONE}, ${TIME_TWO}"
}

prompt_branch() {
    echo ""
    print_info "Выберите ветку репозитория:"
    echo "    1) master (стабильная версия)"
    echo "    2) develop (версия разработки)"
    echo ""

    while true; do
        read -p "  Ваш выбор [1]: " branch_choice
        branch_choice=${branch_choice:-1}

        case $branch_choice in
            1)
                BRANCH="master"
                print_success "Выбрана ветка: master"
                return 0
                ;;
            2)
                BRANCH="develop"
                print_success "Выбрана ветка: develop"
                return 0
                ;;
            *)
                print_warning "Пожалуйста, введите 1 или 2"
                ;;
        esac
    done
}

prompt_news() {
    echo ""
    print_info "Настройка новостного дайджеста"
    echo ""

    if prompt_yes_no "Включить новостной дайджест (RSS ЦБ РФ)?" "N"; then
        NEWS_ENABLED="true"

        read -p "  Время рассылки новостей [${DEFAULT_NEWS_TIME}]: " news_time
        NEWS_TIME="${news_time:-$DEFAULT_NEWS_TIME}"

        print_success "Новостной дайджест включён, время: ${NEWS_TIME}"

        # Важные новости
        echo ""
        print_info "Важные новости — автоматическая рассылка самых цитируемых новостей раз в час"
        if prompt_yes_no "Включить рассылку важных новостей?" "N"; then
            IMPORTANT_NEWS_ENABLED="true"
            print_success "Важные новости включены"
        else
            IMPORTANT_NEWS_ENABLED="false"
            print_success "Важные новости отключены"
        fi
    else
        NEWS_ENABLED="false"
        IMPORTANT_NEWS_ENABLED="false"
        NEWS_TIME="${DEFAULT_NEWS_TIME}"
        print_success "Новостной дайджест отключён"
    fi
}

prompt_llm_provider() {
    echo ""
    print_info "Настройка LLM-провайдера для суммаризации новостей"
    echo ""
    print_info "LLM используется для создания кратких сводок из RSS-новостей ЦБ РФ."
    print_info "Без LLM новости будут показаны как есть (заголовок + ссылка)."
    echo ""
    echo "    1) Отключён (без суммаризации)"
    echo "    2) Polza (облачный, требует API-ключ)"
    echo "    3) Ollama (локальный, требует запущенный сервер)"
    echo ""

    while true; do
        read -p "  Ваш выбор [1]: " llm_choice
        llm_choice=${llm_choice:-1}

        case $llm_choice in
            1)
                LLM_PROVIDER=""
                print_success "LLM-провайдер: отключён"
                return 0
                ;;
            2)
                LLM_PROVIDER="polza"
                print_success "LLM-провайдер: Polza"
                echo ""
                prompt_polza_key
                return 0
                ;;
            3)
                LLM_PROVIDER="ollama"
                print_success "LLM-провайдер: Ollama"
                echo ""
                prompt_ollama_url
                return 0
                ;;
            *)
                print_warning "Пожалуйста, введите 1, 2 или 3"
                ;;
        esac
    done
}

prompt_polza_key() {
    print_info "Введите API-ключ Polza"
    echo ""

    while true; do
        read -p "  API-ключ: " polza_key

        if [ -z "$polza_key" ]; then
            print_warning "API-ключ не может быть пустым"
            if prompt_yes_no "Отключить LLM-провайдер?" "Y"; then
                LLM_PROVIDER=""
                print_success "LLM-провайдер отключён"
                return 0
            fi
            continue
        fi

        POLZA_API_KEY="$polza_key"
        local masked_key="${polza_key:0:4}$(echo "${polza_key:4}" | sed 's/./x/g')"
        print_success "API-ключ принят: ${masked_key}"
        return 0
    done
}

prompt_ollama_url() {
    print_info "Введите URL Ollama сервера"
    echo ""

    read -p "  URL Ollama [${DEFAULT_OLLAMA_URL}]: " ollama_url
    OLLAMA_URL="${ollama_url:-$DEFAULT_OLLAMA_URL}"

    print_success "Ollama URL: ${OLLAMA_URL}"
}

#==================================================================================================
#  Функции работы с репозиторием
#==================================================================================================

check_existing_project() {
    # Проверяем наличие .git папки
    if [ -d ".git" ]; then
        return 0
    fi

    # Проверяем наличие ключевых файлов проекта
    if [ -f "$COMPOSE_FILE" ] && [ -f "$ENV_FILE" ]; then
        return 0
    fi

    return 1
}

clone_or_update_repo() {
    local operation="клонирование"
    local current_dir=$(pwd)

    if check_existing_project; then
        operation="обновление"

        # Проверяем наличие незафиксированных изменений
        if ! git diff-index --quiet HEAD --; then
            print_warning "Обнаружены незафиксированные изменения в репозитории"
            if ! prompt_yes_no "Продолжить обновление? Несохраненные изменения могут быть потеряны"; then
                print_error "Обновление отменено"
                return 1
            fi
        fi

        print_info "Обновление ветки '${BRANCH}'..."
        if ! git checkout "$BRANCH" 2>/dev/null; then
            print_error "Ветка '${BRANCH}' не существует"
            return 1
        fi

        if ! git pull origin "$BRANCH"; then
            print_error "Не удалось обновить репозиторий"
            return 1
        fi
    else
        print_info "Клонирование репозитория (${BRANCH})..."

        if [ -n "$(ls -A)" ]; then
            # Проверяем, существует ли уже папка проекта
            if [ -d "$PROJECT_NAME" ]; then
                print_warning "Текущая папка не пуста"
                print_warning "Папка '${PROJECT_NAME}' уже существует"

                echo ""
                print_info "Что вы хотите сделать?"
                echo "    1) Использовать существующую папку '${PROJECT_NAME}'"
                echo "    2) Отменить операцию"
                echo ""

                while true; do
                    read -p "  Ваш выбор [2]: " use_existing
                    use_existing=${use_existing:-2}

                    case $use_existing in
                        1)
                            # Переходим в существующую папку
                            print_info "Переход в папку '${PROJECT_NAME}'..."
                            cd "$PROJECT_NAME"

                            # Проверяем, что это действительно проект
                            if ! check_existing_project; then
                                print_error "Папка '${PROJECT_NAME}' не содержит проект ExchangeRates.Api"
                                return 1
                            fi

                            # Считываем существующий токен
                            if [ -f "$ENV_FILE" ]; then
                                BOT_TOKEN=$(grep "^BOT_TOKEN=" "$ENV_FILE" | cut -d'=' -f2)
                                if [ -z "$BOT_TOKEN" ] || [ "$BOT_TOKEN" = "YOUR_BOT_TOKEN_HERE" ]; then
                                    BOT_TOKEN=""
                                fi
                            fi

                            operation="обновление"
                            # Обновляем существующий проект
                            print_info "Обновление ветки '${BRANCH}'..."
                            if ! git checkout "$BRANCH" 2>/dev/null; then
                                print_error "Ветка '${BRANCH}' не существует"
                                return 1
                            fi

                            if ! git pull origin "$BRANCH"; then
                                print_error "Не удалось обновить репозиторий"
                                return 1
                            fi

                            return 0
                            ;;
                        2|"")
                            print_error "Операция отменена"
                            return 1
                            ;;
                        *)
                            print_warning "Пожалуйста, введите 1 или 2"
                            ;;
                    esac
                done
            else
                # Папка не существует, спрашиваем создать ли её
                print_warning "Текущая папка не пуста"
                echo ""
                print_info "Создастся отдельная папка '${PROJECT_NAME}' для проекта"
                if ! prompt_yes_no "Продолжить?"; then
                    print_error "Операция отменена"
                    return 1
                fi

                # Создаем папку проекта
                print_info "Создание папки '${PROJECT_NAME}'..."
                if ! mkdir "$PROJECT_NAME"; then
                    print_error "Не удалось создать папку '${PROJECT_NAME}'"
                    return 1
                fi

                # Переходим в папку проекта
                cd "$PROJECT_NAME"
            fi
        fi

        if ! git clone -b "$BRANCH" "$GITHUB_REPO" .; then
            print_error "Не удалось клонировать репозиторий"
            print_info "Проверьте подключение к интернету"
            return 1
        fi
    fi

    print_success "${operation^} завершено"
    return 0
}

#==================================================================================================
#  Функции работы с конфигурацией
#==================================================================================================

backup_existing_env() {
    if [ -f "$ENV_FILE" ]; then
        local backup_file="${ENV_FILE}.backup.$(date +%Y%m%d_%H%M%S)"
        print_info "Создание бэкапа существующего .env: ${backup_file}"
        cp "$ENV_FILE" "$backup_file"
    fi
}

create_env_file() {
    print_info "Создание файла конфигурации..."

    # Если файл уже существует, спрашиваем о перезаписи
    if [ -f "$ENV_FILE" ]; then
        if ! prompt_yes_no "Файл .env уже существует. Перезаписать?"; then
            print_info "Используется существующий файл .env"
            return 0
        fi
        backup_existing_env
    fi

    # Создаем новый файл
    cat > "$ENV_FILE" << EOF
# Токен Telegram бота (получить от @BotFather)
# ВАЖНО: Заменить на реальный токен перед запуском!
BOT_TOKEN=${BOT_TOKEN}

# Режим работы бота
# true = Polling (рекомендуется для локальной разработки и Docker)
# false = Webhook (требует публичный HTTPS URL)
BOT_USE_POLLING=$([ "$BOT_MODE" = "polling" ] && echo "true" || echo "false")

# Webhook URL (используется только если BOT_USE_POLLING=false)
# Оставить пустым в polling режиме
BOT_WEBHOOK=${BOT_WEBHOOK}

# Время рассылки сообщений подписчикам
BOT_TIME_ONE=${TIME_ONE}
BOT_TIME_TWO=${TIME_TWO}

# --- Новостной сервис ---
# Включить новостной дайджест (true/false)
NEWS_ENABLED=${NEWS_ENABLED}

# Время рассылки новостного дайджеста
NEWS_TIME=${NEWS_TIME}

# LLM провайдер для суммаризации новостей: "" (отключен), "polza", "ollama"
LLM_PROVIDER=${LLM_PROVIDER}

# API ключ для Polza LLM (если LLM_PROVIDER=polza)
POLZA_API_KEY=${POLZA_API_KEY}

# URL Ollama сервера (если LLM_PROVIDER=ollama)
OLLAMA_URL=${OLLAMA_URL:-${DEFAULT_OLLAMA_URL}}

# Включить рассылку важных новостей раз в час (true/false)
IMPORTANT_NEWS_ENABLED=${IMPORTANT_NEWS_ENABLED}
EOF

    print_success "Файл $ENV_FILE создан"
}

ensure_gitignore() {
    local env_ignored=$(grep -q "^\.env$" .gitignore 2>/dev/null && echo "true" || echo "false")

    if [ "$env_ignored" = "false" ]; then
        print_warning ".env не найден в .gitignore"
        print_info "Добавление .env в .gitignore..."

        # Проверяем, есть ли .gitignore
        if [ ! -f ".gitignore" ]; then
            touch .gitignore
        fi

        # Добавляем .env, если его нет
        if ! grep -q "^\.env$" .gitignore; then
            echo "" >> .gitignore
            echo "# Environment variables (contains secrets)" >> .gitignore
            echo ".env" >> .gitignore
        fi

        print_success ".env добавлен в .gitignore"
    else
        print_success ".gitignore проверен"
    fi
}

#==================================================================================================
#  Функции Docker
#==================================================================================================

build_docker_images() {
    print_step "6/7" " Сборка Docker образов..."

    if [ ! -f "$COMPOSE_FILE" ]; then
        print_error "Файл $COMPOSE_FILE не найден"
        return 1
    fi

    print_info "Сборка exchangerates-api..."

    if ! docker-compose build 2>&1 | tee /tmp/docker-build.log; then
        print_error "Сборка Docker образов не удалась"
        print_info "Лог сохранен: /tmp/docker-build.log"
        return 1
    fi

    print_success "Сборка завершена"
    return 0
}

start_services() {
    print_step "7/7" " Запуск сервисов..."

    if [ ! -f "$COMPOSE_FILE" ]; then
        print_error "Файл $COMPOSE_FILE не найден"
        return 1
    fi

    print_info "Запуск docker-compose..."

    if ! docker-compose up -d 2>&1 | tee /tmp/docker-start.log; then
        print_error "Запуск сервисов не удался"
        print_info "Лог сохранен: /tmp/docker-start.log"
        return 1
    fi

    # Ждем несколько секунд для инициализации
    sleep 3

    print_success "Сервисы успешно запущены"
    return 0
}

show_status() {
    echo ""
    print_info "Статус контейнеров:"
    echo ""

    if docker-compose ps 2>/dev/null; then
        return 0
    else
        print_warning "Не удалось получить статус контейнеров"
        return 1
    fi
}

show_logs() {
    echo ""
    print_info "Логи (нажмите Ctrl+C для выхода):"
    echo ""

    docker-compose logs -f
}

#==================================================================================================
#  Обработка ошибок
#==================================================================================================

cleanup_on_error() {
    echo ""
    print_warning "Очистка после ошибки..."

    # Останавливаем контейнеры, если они были запущены
    if docker-compose ps -q 2>/dev/null | grep -q .; then
        print_info "Остановка контейнеров..."
        docker-compose down 2>/dev/null || true
    fi

    # Удаляем временные файлы
    rm -f /tmp/docker-build.log 2>/dev/null || true
    rm -f /tmp/docker-start.log 2>/dev/null || true

    print_error "Развертывание завершено с ошибками"
    exit 1
}

# Trap для обработки прерываний
trap cleanup_on_error SIGINT SIGTERM

#==================================================================================================
#  Основная точка входа
#==================================================================================================

main() {
    # Вывод заголовка
    print_header

    # Шаг 1: Проверка зависимостей
    if ! check_dependencies; then
        return 1
    fi

    # Шаг 2: Определение ОС
    if ! check_os; then
        return 1
    fi

    # Шаг 3: Выбор ветки
    print_step "3/7" " Работа с репозиторием..."
    if check_existing_project; then
        print_info "Обнаружен существующий проект: ${PROJECT_NAME}"
        if prompt_yes_no "Обновить существующий проект?"; then
            if ! prompt_branch; then
                return 1
            fi
        else
            # Если не обновляем, проверяем существующий .env
            if [ -f "$ENV_FILE" ]; then
                print_info "Используется существующий .env файл"
                # Извлекаем токен из существующего файла
                BOT_TOKEN=$(grep "^BOT_TOKEN=" "$ENV_FILE" | cut -d'=' -f2)
                if [ -z "$BOT_TOKEN" ] || [ "$BOT_TOKEN" = "YOUR_BOT_TOKEN_HERE" ]; then
                    print_warning "Токен не найден или не настроен"
                    if ! prompt_bot_token; then
                        return 1
                    fi
                fi
            else
                print_info ".env файл не найден"
                if ! prompt_branch; then
                    return 1
                fi
            fi
        fi
    else
        print_info "Новая установка"
        if ! prompt_branch; then
            return 1
        fi
    fi

    # Шаг 3: Работа с репозиторием
    if [ -n "$BRANCH" ]; then
        if ! clone_or_update_repo; then
            return 1
        fi
    fi

    # Шаг 4: Настройка конфигурации
    print_step "4/7" " Настройка конфигурации..."

    # Запрос токена, если он не был получен ранее
    if [ -z "$BOT_TOKEN" ] || [ "$BOT_TOKEN" = "YOUR_BOT_TOKEN_HERE" ]; then
        if ! prompt_bot_token; then
            return 1
        fi
    fi

    # Запрос режима работы
    if ! prompt_bot_mode; then
        return 1
    fi

    # Запрос Webhook URL, если выбран webhook режим
    if [ "$BOT_MODE" = "webhook" ]; then
        if ! prompt_webhook_url; then
            return 1
        fi
    else
        BOT_WEBHOOK=""
    fi

    # Запрос времени рассылки
    prompt_time

    # Запрос настроек новостей
    prompt_news

    # Запрос LLM-провайдера (если новости включены)
    if [ "$NEWS_ENABLED" = "true" ]; then
        prompt_llm_provider
    fi

    # Шаг 5: Создание файла конфигурации
    print_step "5/7" " Создание файла конфигурации..."
    if ! create_env_file; then
        return 1
    fi

    if ! ensure_gitignore; then
        return 1
    fi

    # Шаг 6: Сборка Docker образов
    if ! build_docker_images; then
        return 1
    fi

    # Шаг 7: Запуск сервисов
    if ! start_services; then
        return 1
    fi

    # Шаг 8: Показ статуса
    show_status

    # Шаг 9: Логи (опционально)
    echo ""
    if prompt_yes_no "Показать логи в реальном времени?"; then
        show_logs
    fi

    # Завершение
    print_divider
    print_success "Развертывание завершено!"
    print_divider

    echo ""
    print_success "Сервисы успешно запущены"
    echo ""
    print_info "Следующие шаги:"
    echo "  1. Найдите бота в Telegram"
    echo "  2. Отправьте команду /start"
    echo "  3. Проверьте работоспособность: /valuteoneday"
    if [ "$NEWS_ENABLED" = "true" ]; then
        echo "  4. Настройте подписку на новости: /news"
    fi
    echo ""
    print_info "Управление:"
    echo "  docker-compose logs -f          - просмотр логов"
    echo "  docker-compose ps               - статус контейнеров"
    echo "  docker-compose down             - остановка сервисов"
    echo "  docker-compose up -d --build    - пересборка и запуск"
    echo ""
    print_info "Сервисы:"
    echo "  API:          http://localhost:5000"
    echo "  NewsService:  http://localhost:5002"
    if [ "$NEWS_ENABLED" = "true" ]; then
        echo "  Новости:      включены (LLM: ${LLM_PROVIDER:-отключён})"
    fi
    if [ "$IMPORTANT_NEWS_ENABLED" = "true" ]; then
        echo "  Важные:       включены (раз в час)"
    fi
    echo ""

    return 0
}

#==================================================================================================
#  Запуск
#==================================================================================================
main "$@"
