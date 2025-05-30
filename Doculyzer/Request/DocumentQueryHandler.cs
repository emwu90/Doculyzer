using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Doculyzer.Core.Models;
using Doculyzer.Core.Services;
using Microsoft.Extensions.Logging;

namespace Doculyzer.Request
{
    public class DocumentQueryHandler : IRequestHandler<DocumentQueryRequest, DocumentQueryResult>
    {
        private readonly IAzureOpenAIService _openAIService;
        private readonly IInvoiceSearchRepository _searchRepository;
        private readonly IInvoiceAnalysisService _analysisService;
        private readonly ILogger<DocumentQueryHandler> _logger;

        public DocumentQueryHandler(
            IAzureOpenAIService openAIService,
            IInvoiceSearchRepository searchRepository,
            IInvoiceAnalysisService analysisService,
            ILogger<DocumentQueryHandler> logger)
        {
            _openAIService = openAIService;
            _searchRepository = searchRepository;
            _analysisService = analysisService;
            _logger = logger;
        }

        public async Task<DocumentQueryResult> HandleAsync(DocumentQueryRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Parse the natural language query to extract intent and parameters
                var queryIntent = await _openAIService.ParseQueryIntentAsync(request.Prompt, cancellationToken) ?? throw new InvalidOperationException("Query intent could not be parsed.");

                // Step 2: Search for relevant invoices based on the parsed intent
                var relevantInvoices = await SearchInvoicesAsync(queryIntent, cancellationToken);

                // Step 3: Analyze the invoices to generate the answer
                var answer = await _analysisService.AnalyzeInvoicesForQueryAsync(relevantInvoices, request.Prompt, cancellationToken);

                return new DocumentQueryResult
                {
                    Answer = answer,
                    RelevantInvoices = relevantInvoices,
                    IsSuccessful = true
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error processing document query: {Message}", exception.Message);
                return new DocumentQueryResult
                {
                    ErrorMessage = exception.Message,
                    IsSuccessful = false
                };
            }
        }

        private async Task<List<Invoice>> SearchInvoicesAsync(QueryIntent intent, CancellationToken cancellationToken)
        {
            return intent.QueryType switch
            {
                QueryType.DateRange => await _searchRepository.GetInvoicesByDateRangeAsync(
                    intent.StartDate ?? DateTime.MinValue,
                    intent.EndDate ?? DateTime.MaxValue,
                    cancellationToken),

                QueryType.Customer => await _searchRepository.GetInvoicesByCustomerAsync(
                    intent.CustomerName!,
                    intent.StartDate,
                    intent.EndDate,
                    cancellationToken),

                QueryType.Product => await _searchRepository.SearchInvoicesAsync(
                    intent.SearchTerm!,
                    null,
                    cancellationToken),

                _ => await _searchRepository.SearchInvoicesAsync(
                    intent.SearchTerm ?? "*",
                    null,
                    cancellationToken)
            };
        }
    }
}
