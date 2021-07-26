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

    }
}
