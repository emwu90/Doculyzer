using Doculyzer.Core.Models;

namespace Doculyzer.Core.Services
{
    public interface IAzureOpenAIService
    {
        Task<QueryIntent> ParseQueryIntentAsync(string prompt, CancellationToken cancellationToken = default);

        Task<ResponseResult> GenerateAnswerAsync(string prompt, List<Invoice> invoices, CancellationToken cancellationToken = default);

        Task<bool> IsContentToxicAsync(string content, CancellationToken cancellationToken = default);
    }
}
