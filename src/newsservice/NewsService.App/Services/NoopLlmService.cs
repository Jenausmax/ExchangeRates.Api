using NewsService.Domain.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NewsService.App.Services
{
    public class NoopLlmService : ILlmService
    {
        public bool IsAvailable => false;

        public Task<string> SummarizeAsync(string text, CancellationToken cancel = default)
        {
            return Task.FromResult<string>(null);
        }

        public Task<bool> AreSimilarAsync(string text1, string text2, CancellationToken cancel = default)
        {
            return Task.FromResult(false);
        }
    }
}
