﻿using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Doculyzer.Core.Models;
using Doculyzer.Core.Services;
using Microsoft.Extensions.Logging;
using System.Text;

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
                // Step 1: Check for toxic content in the query
                SanitizeAndValidatePrompt(request.Prompt);

                if (await _openAIService.IsContentToxicAsync(request.Prompt, cancellationToken))
                {
                    _logger.LogWarning("Toxic content detected in query");
                    return new DocumentQueryResult
                    {
                        IsSuccessful = false,
                        ErrorMessage = "The query contains inappropriate content and cannot be processed."
                    };
                }

                // Step 2: Parse the natural language query to extract intent and parameters
                var queryIntent = await _openAIService.ParseQueryIntentAsync(request.Prompt, cancellationToken)
                                  ?? throw new InvalidOperationException("Query intent could not be parsed.");

                // Step 3: Search for relevant invoices based on the parsed intent
                var relevantInvoices = await SearchInvoicesAsync(queryIntent, cancellationToken);

                // Step 4: Analyze the invoices to generate the answer
                var responseResult = await _analysisService.AnalyzeInvoicesForQueryAsync(relevantInvoices, request.Prompt, cancellationToken);

                // Step 5: Check if the answer indicates no response
                if (string.IsNullOrWhiteSpace(responseResult.ResponseText) || responseResult.ResponseText.Contains("cannot answer", StringComparison.OrdinalIgnoreCase))
                {
                    relevantInvoices.Clear();
                }

                return new DocumentQueryResult
                {
                    Answer = responseResult.ResponseText,
                    RelevantInvoices = relevantInvoices,
                    IsSuccessful = true,
                    ResponseId = responseResult.ResponseId
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
            if (intent.QueryType == QueryType.Invalid)
            {
                return [];
            }

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

        private static string SanitizeAndValidatePrompt(string prompt)
        {
            if (prompt.Length < 3)
                throw new InvalidOperationException("Prompt is too short.");

            var lower = prompt.ToLowerInvariant();
            if (lower.Contains("ignore previous") || lower.Contains("disregard above") || lower.Contains("<script>"))
                throw new InvalidOperationException("Prompt contains potentially dangerous content.");

            var sanitized = new string([.. prompt.Where(c => !char.IsControl(c))]).Trim();
            sanitized = sanitized.Normalize(NormalizationForm.FormC);
            return sanitized;
        }
    }
}
