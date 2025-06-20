using Doculyzer.Core.Mediator;
using Doculyzer.Request;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Doculyzer
{
    public class ProcessInvoiceMetadata
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ProcessInvoiceMetadata> _logger;

        public ProcessInvoiceMetadata(
            IMediator mediator,
            ILogger<ProcessInvoiceMetadata> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [Function(nameof(ProcessInvoiceMetadata))]
        public async Task ProcessInvoiceBlob(
            [BlobTrigger("%ServicesConfig:InvoiceContainerName%/{name}", Connection = "ServicesConfig:StorageConnectionString")] Stream blobStream,
            string name,
            CancellationToken cancellationToken)
        {
            if (!name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return;

            _logger.LogInformation("Started processing document added to blob.");

            var command = new ProcessInvoiceMetadataRequest { BlobName = name };
            await _mediator.SendAsync(command, cancellationToken);
        }
    }
}
