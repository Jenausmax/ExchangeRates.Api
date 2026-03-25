- [x] Реализовано

# BOT-0024: Персонализация криптовалют

## Описание

Возможность выбрать, какие криптовалюты показывать при нажатии «Крипто».
По аналогии с персонализацией фиат-валют (`/currencies`).

## Реализовано

### Модель данных
- `UserDb.CryptoCoins` (string, nullable, CSV) — выбранные монеты
- `CurrentUser.CryptoCoins` — маппинг в SetUser
- EF-миграция `AddCryptoCoins` (ALTER TABLE ADD COLUMN TEXT NULL)

### Интерфейсы
- `IUserService.UpdateCryptoCoins(chatId, cryptoCoins, cancel)`
- `IUserService.GetUserCryptoCoins(chatId)` → null = все 10 монет
- `IKriptoApiClient.GetLatestPricesAsync(currency, symbols, cancel)` — новый параметр symbols

### UI
- Кнопка «Монеты» в reply-клавиатуре (3-й ряд: Новости | Крипто | Монеты)
- Команда `/cryptocoins`
- Inline-клавиатура с toggle 10 монет (✅/⬜, по 3 в ряд)
- Callbacks: `toggle_crypto_{SYMBOL}`, `save_crypto_coins`

### Интеграция
- `HandleCryptoCommand` / `HandleCryptoCallback` передают выбранные symbols в KriptoService
- NULL = все 10 монет (обратная совместимость)

## Архитектурные документы
- [ADR](../architect/adr-bot-0024-crypto-personalization.md)
- [Архитектура](../architect/bot-0024-crypto-personalization.md)
