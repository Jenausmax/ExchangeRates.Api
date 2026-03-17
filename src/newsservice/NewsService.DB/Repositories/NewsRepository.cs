using Microsoft.EntityFrameworkCore;
using NewsService.Domain.Interfaces;
using NewsService.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.DB.Repositories
{
    public class NewsRepository : INewsRepository
    {
        private readonly NewsDataDb _db;
        private readonly ILogger _logger;

        public NewsRepository(NewsDataDb db, ILogger logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> ExistsByHashAsync(string contentHash, CancellationToken cancel = default)
        {
            return await _db.Topics.AnyAsync(t => t.ContentHash == contentHash, cancel);
        }

        public async Task<NewsTopicDb> CreateTopicAsync(NewsTopicDb topic, CancellationToken cancel = default)
        {
            await _db.Topics.AddAsync(topic, cancel);
            await _db.SaveChangesAsync(cancel);
            return topic;
        }

        public async Task<NewsItemDb> CreateItemAsync(NewsItemDb item, CancellationToken cancel = default)
        {
            await _db.Items.AddAsync(item, cancel);
            await _db.SaveChangesAsync(cancel);
            return item;
        }

        public async Task<List<NewsTopicDb>> GetUnsentTopicsAsync(int maxCount, CancellationToken cancel = default)
        {
            return await _db.Topics
                .Where(t => !t.IsSent)
                .OrderByDescending(t => t.PublishedAt)
                .Take(maxCount)
                .ToListAsync(cancel);
        }

        public async Task<int> MarkTopicsAsSentAsync(List<int> topicIds, CancellationToken cancel = default)
        {
            var topics = await _db.Topics
                .Where(t => topicIds.Contains(t.Id))
                .ToListAsync(cancel);

            foreach (var topic in topics)
            {
                topic.IsSent = true;
            }

            await _db.SaveChangesAsync(cancel);
            return topics.Count;
        }

        public async Task<int> GetTotalCountAsync(CancellationToken cancel = default)
        {
            return await _db.Topics.CountAsync(cancel);
        }

        public async Task<int> GetUnsentCountAsync(CancellationToken cancel = default)
        {
            return await _db.Topics.CountAsync(t => !t.IsSent, cancel);
        }

        public async Task<DateTime?> GetLastFetchTimeAsync(CancellationToken cancel = default)
        {
            if (!await _db.Topics.AnyAsync(cancel))
                return null;
            return await _db.Topics.MaxAsync(t => t.FetchedAt, cancel);
        }
    }
}
