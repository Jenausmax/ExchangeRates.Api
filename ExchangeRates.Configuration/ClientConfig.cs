using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRates.Configuration
{
    public class ClientConfig
    {
        public string SiteApi { get; set; }
        public string SiteGet { get; set; }
        public bool Logging { get; set; }
        public int PeriodHours { get; set; }
        
    }
}
