- [x] Реализовано

# BOT-0015: Бесконечная лента новостей + исправление Polza AI + новые RSS-источники

## Фича 1: Исправление Polza AI
- PolzaLlmService: try-catch с логированием HTTP-статуса и тела ответа
- Обработка таймаутов (TaskCanceledException)
- Логирование при инициализации (API key status)
- NewsDeduplicationService: логирование LLM availability для каждой новости
- Startup.cs: логирование выбранного LLM-провайдера при старте

## Фича 2: 5 новых финансовых RSS-источников
Добавлены в NewsConfig и appsettings.json:
- РБК: rssexport.rbc.ru/rbcnews/news/30/full.rss
- Интерфакс: www.interfax.ru/rss.asp
- ТАСС: tass.ru/rss/v2.xml
- Коммерсант: www.kommersant.ru/RSS/section-economics.xml
- Banki.ru: www.banki.ru/xml/news.rss

## Фича 3: Бесконечная лента новостей
- Cursor-based пагинация по PublishedAt + Id
- DigestResponse.HasMore для определения наличия следующей страницы
- NewsRepository: GetAllTopicsAsync, GetTopicsBeforeIdAsync (курсор по PublishedAt)
- DigestController: параметры beforeId и all
- Бот: callback news_p_{lastTopicId}, кнопка "Далее ⬇️"
- Показывает ВСЕ новости (не только unsent), сортировка по дате (новые первые)

## Ветка
feature/BOT-0015-news-improvements
