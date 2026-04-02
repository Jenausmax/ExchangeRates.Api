using Microsoft.AspNetCore.Mvc;
using KriptoService.Domain.Dto;
using KriptoService.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KriptoService.Controllers
{
    [Route("api/crypto")]
    [ApiController]
    public class CryptoController : ControllerBase
    {
        private readonly ICryptoService _cryptoService;

        public CryptoController(ICryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<CryptoPriceResponse>> GetLatest(
            [FromQuery] string symbols = null,
            [FromQuery] string currencies = null,
            CancellationToken cancel = default)
        {
            var symbolsArr = string.IsNullOrWhiteSpace(symbols)
                ? null
                : symbols.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToArray();

            var currenciesArr = string.IsNullOrWhiteSpace(currencies)
                ? null
                : currencies.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim().ToUpper()).ToArray();

            var result = await _cryptoService.GetLatestAsync(symbolsArr, currenciesArr, cancel);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult<CryptoHistoryResponse>> GetHistory(
            [FromQuery] string symbol = "BTC",
            [FromQuery] string currency = "RUB",
            [FromQuery] int hours = 24,
            CancellationToken cancel = default)
        {
            if (hours < 1 || hours > 4320)
                return BadRequest("hours must be between 1 and 4320");

            var result = await _cryptoService.GetHistoryAsync(symbol.Trim().ToUpper(), currency.Trim().ToUpper(), hours, cancel);
            return Ok(result);
        }

        [HttpGet("status")]
        public async Task<ActionResult<CryptoStatusResponse>> GetStatus(CancellationToken cancel = default)
        {
            var result = await _cryptoService.GetStatusAsync(cancel);
            return Ok(result);
        }
    }
}
