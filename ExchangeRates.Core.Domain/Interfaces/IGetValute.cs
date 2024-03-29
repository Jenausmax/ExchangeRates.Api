﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRates.Core.Domain.Models.GetModel;

namespace ExchangeRates.Core.Domain.Interfaces
{
    public interface IGetValute
    {
        Task<GetValute> GetValuteDay(string charCode, CancellationToken cancel, int day = 1);
    }
}
