using Doculyzer.Core.Models;

namespace Doculyzer.Core.Services
{
    public interface IAzureOpenAIService
    {
        Task<QueryIntent> ParseQueryIntentAsync(string prompt, CancellationToken cancellationToken = default);

        Task<string> GenerateAnswerAsync(string prompt, List<Invoice> invoices, CancellationToken cancellationToken = default);
    }
}
