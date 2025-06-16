using Azure.AI.ContentSafety;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using OpenAI;

namespace Doculyzer.Core.Infrastructure.Factories
{
    public interface IAzureServiceFactory
    {
        BlobServiceClient CreateBlobServiceClient();

        SearchClient CreateSearchClient();

        DocumentAnalysisClient CreateDocumentAnalysisClient();

        OpenAIClient CreateOpenAIClient();

        ContentSafetyClient CreateContentSafetyClient();

        CosmosClient CreateCosmosClient();
    }
}
