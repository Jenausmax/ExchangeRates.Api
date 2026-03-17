using System.Threading;
using System.Threading.Tasks;

namespace NewsService.Domain.Interfaces
{
    public interface ILlmService
    {
        bool IsAvailable { get; }
        Task<string> SummarizeAsync(string text, CancellationToken cancel = default);
        Task<bool> AreSimilarAsync(string text1, string text2, CancellationToken cancel = default);
    }
}
