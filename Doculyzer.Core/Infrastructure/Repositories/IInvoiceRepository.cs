namespace Doculyzer.Core.Infrastructure.Repositories
{
    public interface IInvoiceRepository
    {
        Task<Stream> GetInvoiceBlobStreamAsync(string blobName, CancellationToken cancellationToken = default);

        Task UpdateInvoiceMetadataAsync(string blobName, Dictionary<string, string> metadata, CancellationToken cancellationToken = default);

        Task<bool> IsProcessedBlobAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
