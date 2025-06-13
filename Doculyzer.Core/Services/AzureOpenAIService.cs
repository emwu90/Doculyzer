using Azure.AI.ContentSafety;
using Doculyzer.Core.Configuration;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Doculyzer.Core.Services
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly ChatClient _chatClient;
        private readonly ContentSafetyClient _contentSafetyClient;
        private readonly ILogger<AzureOpenAIService> _logger;

        public AzureOpenAIService(IAzureServiceFactory serviceFactory, IOptions<ServicesConfig> config, ILogger<AzureOpenAIService> logger)
        {
            _logger = logger;
            var client = serviceFactory.CreateOpenAIClient();
            _chatClient = client.GetChatClient(config.Value.OpenAIDeploymentName);
            _contentSafetyClient = serviceFactory.CreateContentSafetyClient();
        }

        public async Task<QueryIntent> ParseQueryIntentAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var systemPrompt = @"
You are an AI assistant that parses natural language queries about invoices.
Extract the following information from the user's query and return it in JSON format:
- QueryType: 'DateRange', 'Customer', 'Product', or 'General'
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

        public async Task<string> GenerateAnswerAsync(string prompt, List<Invoice> invoices, CancellationToken cancellationToken = default)
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

            var response = await _chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
            var responseText = response.Value.Content.Last().Text;

            if (await IsContentToxicAsync(responseText, cancellationToken))
            {
                _logger.LogWarning("Toxic content detected in AI response: {Response}", responseText);
                return "The response contains inappropriate content and has been filtered.";
            }

            return responseText;
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
    }
}
