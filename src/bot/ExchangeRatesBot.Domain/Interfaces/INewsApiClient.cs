using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRatesBot.Domain.Interfaces
{
    public interface INewsApiClient
    {
        /// <summary>
        /// Получить последний дайджест новостей от NewsService
        /// </summary>
        Task<NewsDigestResult> GetLatestDigestAsync(int maxNews = 10, CancellationToken cancel = default);

        /// <summary>
        /// Получить дайджест новостей, опубликованных после указанного момента времени
        /// </summary>
        Task<NewsDigestResult> GetDigestSinceAsync(DateTime since, int maxNews = 5, CancellationToken cancel = default);

        /// <summary>
        /// Пометить темы как отправленные
        /// </summary>
        Task MarkSentAsync(List<int> topicIds, CancellationToken cancel = default);

        /// <summary>
        /// Получить статус NewsService
        /// </summary>
        Task<NewsServiceStatus> GetStatusAsync(CancellationToken cancel = default);
    }

    public class NewsDigestResult
    {
        public string Message { get; set; }
        public List<int> TopicIds { get; set; }
    }

    public class NewsServiceStatus
    {
        public int TotalTopics { get; set; }
        public int UnsentTopics { get; set; }
        public DateTime? LastFetchTime { get; set; }
    }
}
