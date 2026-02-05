using ExchangeRatesBot.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ExchangeRatesBot.Controllers
{
    [Route("")]
    [ApiController]
    public class UpdateController : ControllerBase
    {
        private readonly ICommandBot _commandBot;

        public UpdateController(ICommandBot commandServiceBot)
        {
            _commandBot = commandServiceBot;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            //await _commandBot.SetUpdateBot(update);
            await _commandBot.SetCommandBot(update);
            return Ok();
        }
    }
}
