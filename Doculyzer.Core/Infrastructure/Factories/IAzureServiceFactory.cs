using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using OpenAI;

namespace Doculyzer.Core.Infrastructure.Factories
{
    public interface IAzureServiceFactory
    {
        BlobServiceClient CreateBlobServiceClient();

        SearchClient CreateSearchClient();

        DocumentAnalysisClient CreateDocumentAnalysisClient();

        OpenAIClient CreateOpenAIClient();
    }
}
