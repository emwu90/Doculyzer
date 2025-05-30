namespace Doculyzer.Core.Configuration
{
    public class ServicesConfig
    {

        public required string StorageConnectionString { get; set; }

        public required string InvoiceContainerName { get; set; }

        public required string OpenAIEndpoint { get; set; }

        public required string OpenAIApiKey { get; set; }

        public required string OpenAIDeploymentName { get; set; }

        public required string DocumentIntelligenceEndpoint { get; set; }

        public required string DocumentIntelligenceApiKey { get; set; }

        public required string SearchServiceEndpoint { get; set; }

        public required string SearchServiceApiKey { get; set; }

        public required string SearchIndexName { get; set; }
    }
}
