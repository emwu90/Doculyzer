using Azure.Storage.Blobs;
using Doculyzer.Core.Configuration;
using Doculyzer.Core.Infrastructure.Factories;
using Microsoft.Extensions.Options;

namespace Doculyzer.Core.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly IAzureServiceFactory _serviceFactory;
        private readonly ServicesConfig _config;

        public InvoiceRepository(IAzureServiceFactory serviceFactory, IOptions<ServicesConfig> config)
        {
            _serviceFactory = serviceFactory;
            _config = config.Value;
        }

        public async Task<Stream> GetInvoiceBlobStreamAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = GetBlobClient(blobName);

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
            var seekableStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(seekableStream, cancellationToken);
            seekableStream.Position = 0;
            return seekableStream;
        }

        public async Task UpdateInvoiceMetadataAsync(string blobName, Dictionary<string, string> metadata, CancellationToken cancellationToken = default)
        {
            var blobClient = GetBlobClient(blobName);
            await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
        }

        public async Task<bool> IsProcessedBlobAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = GetBlobClient(blobName);

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return properties.Value.Metadata.TryGetValue("Status", out var status) && status == "Processed";
        }

        private BlobClient GetBlobClient(string blobName)
        {
            var blobServiceClient = _serviceFactory.CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(_config.InvoiceContainerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            return blobClient;
        }
    }
}
