using Doculyzer.Core.Mediator;
using Doculyzer.Request;
using Microsoft.Azure.Functions.Worker;

namespace Doculyzer
{
    public class ProcessInvoiceMetadata
    {
        private readonly IMediator _mediator;

        public ProcessInvoiceMetadata(IMediator mediator)
        {
            _mediator = mediator;
        }

        [Function(nameof(ProcessInvoiceMetadata))]
        public async Task ProcessInvoiceBlob(
            [BlobTrigger("%ServicesConfig:InvoiceContainerName%/{name}", Connection = "ServicesConfig:StorageConnectionString")] Stream blobStream,
            string name,
            CancellationToken cancellationToken)
        {
            if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return;

            var command = new ProcessInvoiceMetadataRequest { BlobName = name };
            await _mediator.SendAsync(command, cancellationToken);
        }
    }
}
