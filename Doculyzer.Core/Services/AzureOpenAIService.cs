using Azure.AI.ContentSafety;
using Doculyzer.Core.Configuration;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Doculyzer.Core.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly ContentSafetyClient _contentSafetyClient;
        private readonly ILogger<AzureOpenAIService> _logger;
        private readonly IEvaluationMetricsRepository _evaluationMetricsRepository;

        public AzureOpenAIService(
            IAzureServiceFactory serviceFactory,
            IEvaluationMetricsRepository evaluationMetricsRepository,
            IOptions<ServicesConfig> config,
            ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;
            _evaluationMetricsRepository = evaluationMetricsRepository;
            var client = serviceFactory.CreateOpenAIClient();
            _chatClient = client.GetChatClient(config.Value.OpenAIDeploymentName);
            _contentSafetyClient = serviceFactory.CreateContentSafetyClient();
        }

        public async Task<QueryIntent> ParseQueryIntentAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var systemPrompt = @"
You are an AI assistant that parses natural language queries about invoices.
Extract the following information from the user's query and return it in JSON format:
- QueryType: 'DateRange', 'Customer', 'Product', 'General' or 'Invalid' if query is vague, nonsensical, or unanswerable
- StartDate: ISO date if mentioned
- EndDate: ISO date if mentioned
- CustomerName: customer identifier if mentioned
- SearchTerm: relevant search terms

Examples:
'What's today amount of invoices in March?' -> {""QueryType"": ""DateRange"", ""StartDate"": ""2024-03-01"", ""EndDate"": ""2024-03-31""}
'Give me list of products sold to customer XYZ in April' -> {""QueryType"": ""Customer"", ""CustomerName"": ""XYZ"", ""StartDate"": ""2024-04-01"", ""EndDate"": ""2024-04-30""}
"
            ;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(prompt)
            };
            var chatCompletionOptions = new ChatCompletionOptions()
            {
                Temperature = (float)0.7,
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            var jsonResponse = response.Value.Content.Last().Text;

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            return JsonSerializer.Deserialize<QueryIntent>(jsonResponse, options) ?? new QueryIntent { QueryType = QueryType.General };
        }

        public async Task<ResponseResult> GenerateAnswerAsync(string prompt, List<Invoice> invoices, CancellationToken cancellationToken = default)
        {
            var invoiceContext = JsonSerializer.Serialize(invoices, new JsonSerializerOptions { WriteIndented = true });

            var systemPrompt = @"
You are an AI assistant that answers questions about invoices.
Use the provided invoice data to answer the user's question accurately.
Provide specific numbers, dates, and details when available.
If you cannot answer based on the provided data, say so clearly and include 'cannot answer' phrase in your response.
"
            ;

            var userMessage = $"Question: {prompt}\n\nInvoice Data:\n{invoiceContext}";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var chatCompletionOptions = new ChatCompletionOptions()
            {
                Temperature = 0.3f,
            };

            var stopwatch = Stopwatch.StartNew();
            var response = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            stopwatch.Stop();

            var responseText = response.Value.Content.Last().Text;

            var evaluationMetrics = await EvaluateResponseAsync(prompt, responseText, invoices, stopwatch.Elapsed, cancellationToken);
            await _evaluationMetricsRepository.SaveMetricsAsync(evaluationMetrics, cancellationToken);

            if (await IsContentToxicAsync(responseText, cancellationToken))
            {
                _logger.LogWarning("Toxic content detected in AI response.");
                return new ResponseResult { ResponseText = "The response contains inappropriate content and has been filtered." };
            }

            return new ResponseResult { ResponseId = evaluationMetrics.Id, ResponseText = responseText };
        }

        public async Task<bool> IsContentToxicAsync(string content, CancellationToken cancellationToken = default)
        {
            var analysisResult = await _contentSafetyClient.AnalyzeTextAsync(
                new AnalyzeTextOptions(content)
                {
                    Categories = { TextCategory.Hate, TextCategory.Sexual, TextCategory.Violence }
                },
                cancellationToken);

            return analysisResult.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Hate)?.Severity > 2 ||
                   analysisResult.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Sexual)?.Severity > 2 ||
                   analysisResult.Value.CategoriesAnalysis.FirstOrDefault(a => a.Category == TextCategory.Violence)?.Severity > 2;
        }

        public async Task<EvaluationMetrics> EvaluateResponseAsync(string query, string response, List<Invoice> invoices, TimeSpan latency, CancellationToken cancellationToken = default)
        {
            var invoiceContext = JsonSerializer.Serialize(invoices, new JsonSerializerOptions { WriteIndented = true });

            var systemPrompt = @"
You are an AI assistant tasked with evaluating the quality of responses to invoice-related queries. Your task is to assess the quality of a response based on the retrieved documents and the original query.
Evaluate the response using the invoice data and the query. If the query is vague, nonsensical, or unanswerable, and the response does not provide meaningful information, assign a score 0 if not provide scores from 0.0 to 1.0 for each of the following:
- Groundedness: Is the response factually supported by the retrieved invoices?
- Relevance: Does the response use information that is relevant to the query?
- Completeness: Does the response fully answer all parts of the query?

Examples:

Query: 'What's the total amount of invoices in 2015?'
Response: 'The total amount of invoices is 257,054.0 EUR.'
Invoice Data: [{""InvoiceNumber"": ""Invoice123"", ""CustomerName"": ""XYZ""}, {""InvoiceNumber"": ""Invoice456"", ""CustomerName"": ""XYZ""}]
Evaluation: {""Groundedness"": 1, ""Relevance"": 1, ""Completeness"": 1}

Query: 'Find all invoices for customer XYZ'
Response: 'Invoices for customer XYZ: Invoice123'
Invoice Data: [{""InvoiceNumber"": ""Invoice123"", ""CustomerName"": ""XYZ""}, {""InvoiceNumber"": ""Invoice456"", ""CustomerName"": ""XYZ""}]
Evaluation: {""Groundedness"": 1, ""Relevance"": 1, ""Completeness"": 0.5}

Query: 'What is the color of the sky?'
Response: 'I cannot answer your question based on the provided invoice data. There is no information about the color of the sky in the invoice data.'
Invoice Data: []
Evaluation: {""Groundedness"": 0, ""Relevance"": 0, ""Completeness"": 0}

Provide the evaluation in JSON format:
{
    ""Groundedness"": <value>,
    ""Relevance"": <value>,
    ""Completeness"": <value>
    ""Latency"": <value>
    ""Query"": <value>
    ""Response"": <value>
}
";

            var userMessage = $@"
Query: {query}
Response: {response}
Invoice Data: {invoiceContext}
Latency: {latency.TotalMilliseconds}
";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            };

            var chatCompletionOptions = new ChatCompletionOptions()
            {
                Temperature = 0.3f,
            };

            var evaluationResponse = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            var evaluationJson = evaluationResponse.Value.Content.Last().Text;

            var llmScores = JsonSerializer.Deserialize<EvaluationMetrics>(evaluationJson) ?? new EvaluationMetrics
            {
                Groundedness = 0,
                Relevance = 0,
                Completeness = 0,
                Latency = latency.TotalMilliseconds,
                Timestamp = DateTime.UtcNow,
            };

            var groundedness = (llmScores.Groundedness + CalculateGroundedness(response, invoices)) / 2;
            var completeness = (llmScores.Completeness + CalculateCompleteness(response, query)) / 2;

            return new EvaluationMetrics
            {
                Query = llmScores.Query,
                Response = llmScores.Response,
                Groundedness = groundedness,
                Relevance = llmScores.Relevance,
                Completeness = completeness,
                Latency = latency.TotalMilliseconds,
                Timestamp = DateTime.UtcNow
            };
        }

        private static double CalculateGroundedness(string response, List<Invoice> invoices)
        {
            var invoiceNumbers = invoices.Select(i => i.InvoiceNumber).ToList();
            return invoiceNumbers.Any(num => response.Contains(num)) ? 1.0 : 0.0;
        }

        private static double CalculateCompleteness(string response, string query)
        {
            var requiredKeywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var matchedKeywords = requiredKeywords.Count(keyword => response.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            return (double)matchedKeywords / requiredKeywords.Length;
        }
    }
}
