using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExchangeRatesBot.DB.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRatesBot.DB
{
    public class DataDb : DbContext
    {
        public DataDb(DbContextOptions<DataDb> options) : base(options)
        {
            
        }

        public DbSet<UserDb> Users { get; set; }
    }
}
