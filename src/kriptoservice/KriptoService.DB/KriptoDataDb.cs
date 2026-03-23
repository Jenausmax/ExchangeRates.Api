using Microsoft.EntityFrameworkCore;
using KriptoService.Domain.Models;

namespace KriptoService.DB
{
    public class KriptoDataDb : DbContext
    {
        public KriptoDataDb(DbContextOptions<KriptoDataDb> options) : base(options) { }

        public DbSet<CryptoPriceDb> Prices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CryptoPriceDb>(entity =>
            {
                entity.HasIndex(e => new { e.Symbol, e.Currency, e.FetchedAt });
                entity.HasIndex(e => e.FetchedAt);
            });
        }
    }
}
