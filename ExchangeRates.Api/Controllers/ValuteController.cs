using ExchangeRates.Core.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task<IActionResult> Get(string charCode, int day)
        {
            _logger.Information("Request valute");
            var res = await _valute.GetValuteDay(charCode, CancellationToken.None, day: day);
            return new JsonResult(res);
        }
    }
}
