using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models;
using Serilog;

namespace ExchangeRates.Api.Controllers
{
    [Route("")]
    [ApiController]
    public class ValuteController : ControllerBase
    {
        private readonly IGetValute _valute;
        private readonly ILogger _logger;

        public ValuteController(IGetValute valute, ILogger logger)
        {
            _valute = valute;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> GetUSD(string charCode, int day)
        {
            _logger.Information("Запрос валюты USD");
            var res = await _valute.GetValuteDay(charCode, CancellationToken.None, day: day);
            return new JsonResult(res);
        }
    }
}
