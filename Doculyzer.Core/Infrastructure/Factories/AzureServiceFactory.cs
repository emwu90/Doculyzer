using Azure;
using Azure.AI.ContentSafety;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Doculyzer.Core.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using OpenAI;

namespace Doculyzer.Core.Infrastructure.Factories
{
    public class AzureServiceFactory : IAzureServiceFactory
    {
        private readonly ServicesConfig _config;

        public AzureServiceFactory(IOptions<ServicesConfig> config)
        {
            _config = config.Value;
        }

        public BlobServiceClient CreateBlobServiceClient()
        {
            return new BlobServiceClient(_config.StorageConnectionString);
        }

        public SearchClient CreateSearchClient()
        {
            var credential = new AzureKeyCredential(_config.SearchServiceApiKey);
            return new SearchClient(new Uri(_config.SearchServiceEndpoint), _config.SearchIndexName, credential);
        }

        public DocumentAnalysisClient CreateDocumentAnalysisClient()
        {
            var credential = new AzureKeyCredential(_config.DocumentIntelligenceApiKey);
            return new DocumentAnalysisClient(new Uri(_config.DocumentIntelligenceEndpoint), credential);
        }

        public OpenAIClient CreateOpenAIClient()
        {
            return new AzureOpenAIClient(new Uri(_config.OpenAIEndpoint), new AzureKeyCredential(_config.OpenAIApiKey));
        }

        public ContentSafetyClient CreateContentSafetyClient()
        {
            var credential = new AzureKeyCredential(_config.ContentSafetyApiKey);
            return new ContentSafetyClient(new Uri(_config.ContentSafetyEndpoint), credential);
        }

        public CosmosClient CreateCosmosClient()
        {
            var options = new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            return new CosmosClient(_config.CosmosDBEndpoint, _config.CosmosDBPrimaryKey, options);
        }
    }
}
