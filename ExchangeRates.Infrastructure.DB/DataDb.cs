using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                .HasIndex(x => x.Time);
        }

    }
}
