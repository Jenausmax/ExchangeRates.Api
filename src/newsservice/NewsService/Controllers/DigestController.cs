using Microsoft.AspNetCore.Mvc;
using NewsService.Domain.Dto;
using NewsService.Domain.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Controllers
{
    [Route("api/digest")]
    [ApiController]
    public class DigestController : ControllerBase
    {
        private readonly INewsDigestService _digestService;

        public DigestController(INewsDigestService digestService)
        {
            _digestService = digestService;
        }

        [HttpGet("latest")]
        public async Task<ActionResult<DigestResponse>> GetLatest(
            [FromQuery] int maxNews = 10,
            [FromQuery] DateTime? since = null,
            CancellationToken cancel = default)
        {
            DigestResponse result;
            if (since.HasValue)
            {
                result = await _digestService.GetDigestSinceAsync(since.Value, maxNews, cancel);
            }
            else
            {
                result = await _digestService.GetLatestDigestAsync(maxNews, cancel);
            }
            return Ok(result);
        }

        [HttpPost("mark-sent")]
        public async Task<ActionResult> MarkSent([FromBody] MarkSentRequest request, CancellationToken cancel = default)
        {
            if (request?.TopicIds == null || request.TopicIds.Count == 0)
                return BadRequest("TopicIds is required");

            var marked = await _digestService.MarkAsSentAsync(request.TopicIds, cancel);
            if (marked == 0)
                return NotFound("No topics found with the provided IDs");

            return Ok(new { MarkedCount = marked });
        }

        [HttpGet("status")]
        public async Task<ActionResult<ServiceStatusResponse>> GetStatus(CancellationToken cancel = default)
        {
            var result = await _digestService.GetStatusAsync(cancel);
            return Ok(result);
        }
    }
}
