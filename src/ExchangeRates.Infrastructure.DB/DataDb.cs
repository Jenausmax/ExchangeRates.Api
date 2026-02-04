using ExchangeRates.Infrastructure.DB.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRates.Infrastructure.DB
{
    public class DataDb : DbContext
    {
        public DataDb(DbContextOptions<DataDb> options) : base(options){}

        private DbSet<ValuteModelDb> Valutes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ValuteModelDb>()
                .HasIndex(x => x.DateSave);

            modelBuilder.Entity<ValuteModelDb>()
                .HasIndex(x => x.ValuteId);

            modelBuilder.Entity<ValuteModelDb>()
                .HasIndex(x => x.CharCode);
        }

    }
}
