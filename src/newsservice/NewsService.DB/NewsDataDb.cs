using Microsoft.EntityFrameworkCore;
using NewsService.Domain.Models;

namespace NewsService.DB
{
    public class NewsDataDb : DbContext
    {
        public NewsDataDb(DbContextOptions<NewsDataDb> options) : base(options) { }

        public DbSet<NewsTopicDb> Topics { get; set; }
        public DbSet<NewsItemDb> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NewsTopicDb>(entity =>
            {
                entity.HasIndex(e => e.ContentHash).IsUnique();
                entity.HasIndex(e => e.IsSent);
                entity.HasIndex(e => e.PublishedAt);
                entity.HasIndex(e => e.SourceCount);
            });

            modelBuilder.Entity<NewsItemDb>(entity =>
            {
                entity.HasOne(e => e.Topic)
                      .WithMany()
                      .HasForeignKey(e => e.TopicId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
