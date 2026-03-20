# BOT-0017: Команда /important — быстрая подписка на важные новости

- [ ] Не реализовано

## Описание

Команда `/important` — однокомандный toggle для подписки на важные новости.
Сейчас путь: `/news` → inline-меню → кнопка «Важные новости: вкл/выкл» (2 шага).
С `/important` — один шаг.

## Поведение

1. Пользователь отправляет `/important`
2. Бот проверяет текущее значение `UserDb.ImportantNewsSubscribe`
3. Если `false` → ставит `true`, отвечает: **«Подписка на важные новости включена! Вы будете получать самые обсуждаемые новости раз в час.»**
4. Если `true` → ставит `false`, отвечает: **«Подписка на важные новости отключена.»**

## Затрагиваемые файлы

### Bot

| Файл | Изменение |
|------|-----------|
| `CommandService.cs` → `MessageCommand()` | Добавить `case "/important":` — вызов toggle-логики |
| `CommandService.cs` → `MessageCommand()` | Добавить кнопку «Важные новости» в reply-клавиатуру (опционально) |
| `BotPhrases.cs` | 2 новых фразы: `ImportantSubscribed`, `ImportantUnsubscribed` |

### Не затрагивается

- `UserService` — метод `ImportantNewsSubscribeUpdate` уже существует (BOT-0016)
- `UserDb` — поле `ImportantNewsSubscribe` уже есть (BOT-0016)
- `NewsService` — изменений не требуется
- Миграции — не нужны
- Docker/конфиг — не нужны

## План реализации

1. **BotPhrases.cs** — добавить 2 строковых свойства
2. **CommandService.cs** — добавить `case "/important":` в switch `MessageCommand()`:
   - Прочитать `_userControl.CurrentUser.ImportantNewsSubscribe`
   - Вызвать `_userControl.ImportantNewsSubscribeUpdate(!current)`
   - Отправить соответствующую фразу
3. **(Опционально)** Добавить кнопку «Важные» в reply-клавиатуру (3-й ряд рядом с «Новости»)
4. Обновить `CLAUDE.md` — добавить `/important` в список команд бота

## Оценка сложности

Минимальная — ~15 строк кода в 2 файлах. Вся инфраструктура уже готова из BOT-0016.
