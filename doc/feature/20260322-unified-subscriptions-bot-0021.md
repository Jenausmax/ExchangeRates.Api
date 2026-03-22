# BOT-0021: Объединение подписок под одну кнопку

- [ ] Не реализовано

## Контекст

Сейчас подписки разбросаны по двум кнопкам reply-клавиатуры: «Подписка» (только курсы) и «Новости» (дайджест + расписание + важные новости + просмотр). Цель — объединить все подписки под одну кнопку «Подписка», упростить навигацию и сократить количество кнопок.

## Текущее состояние

**Reply-клавиатура (3 ряда):**
```
[ Курс сегодня | За 7 дней | Статистика ]
[ Валюты       | Подписка  | Помощь     ]
[ Новости                               ]
```

**Кнопка «Подписка»** → inline: Подписаться | Отписаться (только валютные курсы)

**Кнопка «Новости»** → inline: Последние новости | Настроить расписание | Важные вкл/выкл | Подписаться/Отписаться

## Новое поведение

### Reply-клавиатура (3 ряда)

```
[ Курс сегодня | За 7 дней | Статистика ]
[ Валюты       | Подписка  | Помощь     ]
[ Новости                               ]
```

Кнопка «Новости» остаётся — при нажатии **сразу показывает ленту последних новостей** (без промежуточного меню).

### Кнопка «Подписка» → Главное меню подписок

Inline-меню с **текущим статусом** каждой подписки:

```
  Управление подписками:

  [ ✅ Курсы валют          ]    ← toggle, одно нажатие
  [ Новостной дайджест  ▶  ]    ← открывает подменю
  [ ❌ Важные новости       ]    ← toggle, одно нажатие
```

- `✅` / `❌` — показывает текущий статус подписки
- Нажатие на «Курсы валют» — сразу toggle (подписка ↔ отписка), меню обновляется через EditMessage
- Нажатие на «Важные новости» — сразу toggle, меню обновляется
- Нажатие на «Новостной дайджест ▶» — открывает подменю

### Подменю «Новостной дайджест»

```
  Новостной дайджест:

  [ Подписаться          ]    ← если не подписан
  [ Отписаться           ]    ← если подписан
  [ Настроить расписание ]
  [ ← Назад              ]
```

Или вариант с toggle + расписание:

```
  Новостной дайджест:

  [ ✅ Подписка активна    ]    ← toggle
  [ Настроить расписание  ]
  [ ← Назад               ]
```

### Кнопка «Новости» → Просмотр новостей

Остаётся как есть — показывает последние новости (вызов `news_latest`). Без меню подписок.

## Файлы для изменения

### 1. `src/bot/ExchangeRatesBot.App/Services/CommandService.cs`

**Новые callback-и:**
- `sub_menu` — показать главное меню подписок
- `sub_toggle_rates` — toggle подписки на курсы валют
- `sub_toggle_important` — toggle подписки на важные новости
- `sub_news_menu` — показать подменю новостного дайджеста
- `sub_news_toggle` — toggle подписки на новостной дайджест
- `sub_news_schedule` — открыть расписание (переиспользуем существующую логику `news_schedule`)
- `sub_back` — вернуться в главное меню подписок

**Изменения:**

1. **Метод `Menu()`** → заменить на `SubscriptionMenu(long chatId)` — генерирует inline-меню с актуальным статусом подписок пользователя

2. **Метод `NewsMenu()`** → упростить, оставить только просмотр новостей (или убрать совсем, если кнопка «Новости» будет сразу показывать ленту)

3. **Обработка `/subscribe`** (строки 286-293) → вызывать `SubscriptionMenu(chatId)` вместо `Menu()`

4. **Обработка `/news`** (строки 324-331) → вместо `NewsMenu()` сразу показывать последние новости (как `news_latest`)

5. **`CallbackMessageCommand()`** — добавить обработку новых callback-ов, убрать старые (`news_subscribe`, `news_unsubscribe`, `important_news_subscribe`, `important_news_unsubscribe`)

**Новый метод `SubscriptionMenu()`:**

```csharp
private List<List<InlineKeyboardButton>> SubscriptionMenu(long chatId)
{
    var user = _userControl.CurrentUser;
    var ratesStatus = user?.Subscribe == true ? "✅" : "❌";
    var importantStatus = user?.ImportantNewsSubscribe == true ? "✅" : "❌";

    return new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData($"{ratesStatus} Курсы валют", "sub_toggle_rates") },
        new() { InlineKeyboardButton.WithCallbackData("Новостной дайджест ▶", "sub_news_menu") },
        new() { InlineKeyboardButton.WithCallbackData($"{importantStatus} Важные новости", "sub_toggle_important") }
    };
}
```

**Новый метод `NewsSubscriptionMenu()`:**

```csharp
private List<List<InlineKeyboardButton>> NewsSubscriptionMenu(long chatId)
{
    var user = _userControl.CurrentUser;
    var newsStatus = user?.NewsSubscribe == true ? "✅" : "❌";

    return new List<List<InlineKeyboardButton>>
    {
        new() { InlineKeyboardButton.WithCallbackData($"{newsStatus} Подписка", "sub_news_toggle") },
        new() { InlineKeyboardButton.WithCallbackData("Настроить расписание", "news_schedule") },
        new() { InlineKeyboardButton.WithCallbackData("← Назад", "sub_back") }
    };
}
```

**Обработка callback-ов (в `CallbackMessageCommand()`):**

```csharp
case "sub_toggle_rates":
    var currentRates = _userControl.CurrentUser?.Subscribe == true;
    await _userControl.SubscribeUpdate(chatId, !currentRates);
    // EditMessage — обновляем inline-меню с новым статусом
    await _botService.Client.EditMessageReplyMarkupAsync(chatId, messageId,
        new InlineKeyboardMarkup(SubscriptionMenu(chatId)));
    await _botService.Client.AnswerCallbackQueryAsync(callbackId,
        currentRates ? "Подписка на курсы отменена" : "Подписка на курсы оформлена");
    break;

case "sub_toggle_important":
    var currentImportant = _userControl.CurrentUser?.ImportantNewsSubscribe == true;
    await _userControl.ImportantNewsSubscribeUpdate(chatId, !currentImportant, cancel);
    await _botService.Client.EditMessageReplyMarkupAsync(chatId, messageId,
        new InlineKeyboardMarkup(SubscriptionMenu(chatId)));
    await _botService.Client.AnswerCallbackQueryAsync(callbackId,
        currentImportant ? "Подписка на важные новости отменена" : "Подписка на важные новости оформлена");
    break;

case "sub_news_menu":
    await _botService.Client.EditMessageTextAsync(chatId, messageId,
        "Новостной дайджест:",
        replyMarkup: new InlineKeyboardMarkup(NewsSubscriptionMenu(chatId)));
    await _botService.Client.AnswerCallbackQueryAsync(callbackId);
    break;

case "sub_news_toggle":
    var currentNews = _userControl.CurrentUser?.NewsSubscribe == true;
    if (currentNews) {
        await _userControl.UpdateNewsTimes(chatId, null, cancel);
        await _userControl.NewsSubscribeUpdate(chatId, false, cancel);
    } else {
        await _userControl.UpdateNewsTimes(chatId, "09:00", cancel);
        await _userControl.NewsSubscribeUpdate(chatId, true, cancel);
    }
    await _botService.Client.EditMessageReplyMarkupAsync(chatId, messageId,
        new InlineKeyboardMarkup(NewsSubscriptionMenu(chatId)));
    await _botService.Client.AnswerCallbackQueryAsync(callbackId,
        currentNews ? "Подписка на новости отменена" : "Подписка на новости оформлена (09:00)");
    break;

case "sub_back":
    await _botService.Client.EditMessageTextAsync(chatId, messageId,
        BotPhrases.SubscriptionMenuHeader,
        replyMarkup: new InlineKeyboardMarkup(SubscriptionMenu(chatId)));
    await _botService.Client.AnswerCallbackQueryAsync(callbackId);
    break;
```

### 2. `src/bot/ExchangeRatesBot.App/Phrases/BotPhrases.cs`

Добавить:
```csharp
public static string SubscriptionMenuHeader { get; } = "Управление подписками:";
public static string NewsDigestMenuHeader { get; } = "Новостной дайджест:";
```

Старые фразы (`StartMenu` с текстом про подписку) — обновить текст.

### 3. Удаление старого кода

- Метод `Menu()` — удалить (заменён на `SubscriptionMenu()`)
- Метод `NewsMenu()` — удалить (подписочная часть в `SubscriptionMenu()`, просмотр — напрямую)
- Callback-и `news_subscribe`, `news_unsubscribe`, `important_news_subscribe`, `important_news_unsubscribe`, `"Подписаться"`, `"Отписаться"` — удалить старые обработчики

### 4. Сохранение обратной совместимости

- `news_schedule`, `toggle_news_*`, `save_news_schedule` — **оставить как есть**, переиспользуем
- `news_latest`, `news_p_*` — **оставить как есть**, используются кнопкой «Новости» и пагинацией

## Проверка

1. Кнопка «Подписка» → показывает 3 строки с актуальным статусом
2. Toggle курсов → меню обновляется, статус переключается ✅ ↔ ❌
3. Toggle важных новостей → аналогично
4. «Новостной дайджест ▶» → подменю с toggle + расписание + назад
5. «← Назад» → возврат в главное меню подписок
6. Кнопка «Новости» → показывает последние новости (без меню подписок)
7. Фоновые задачи (JobsSendMessageUsers, JobsSendNewsDigest, JobsSendImportantNews) — работают без изменений
