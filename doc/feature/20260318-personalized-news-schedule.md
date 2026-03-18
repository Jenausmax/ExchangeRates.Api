- [x] Реализовано

# Персонализированное расписание новостей (BOT-0014)

## Что реализовано

### NewsService API
- Добавлен параметр `since` в `GET /api/digest/latest?since=...`
- `INewsRepository.GetTopicsSinceAsync()` — фильтрация по `PublishedAt > since`
- `INewsDigestService.GetDigestSinceAsync()` — получение дайджеста с момента since
- Обратная совместимость: без `since` работает как раньше (по `IsSent`)

### Модель данных бота
- `UserDb.NewsTimes` (string, nullable) — CSV слотов расписания ("09:00,18:00")
- `UserDb.LastNewsDeliveredAt` (DateTime?, nullable) — время последней доставки
- Миграция `AddPersonalizedNewsSchedule` с миграцией данных (NewsSubscribe=1 → NewsTimes="09:00")

### UI Telegram
- Обновлённая команда `/news` с кнопкой "Настроить расписание"
- Экран выбора расписания: 6 слотов (06:00, 09:00, 12:00, 15:00, 18:00, 21:00)
- Toggle-кнопки с ✅ для активных слотов
- Кнопка "Сохранить" для записи в БД
- Подписка ставит дефолт "09:00", отписка очищает NewsTimes

### Per-user рассылка (JobsSendNewsDigest)
- Каждую минуту проверяет пользователей с совпадающим слотом в NewsTimes
- Для каждого запрашивает персональный дайджест с `since=LastNewsDeliveredAt`
- Обновляет `LastNewsDeliveredAt` после успешной отправки
- Не использует глобальный mark-sent (per-user tracking)
