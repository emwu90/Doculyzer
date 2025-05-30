using Doculyzer.Core.Mediator;

namespace Doculyzer.Request
{
    public class ProcessInvoiceMetadataRequest : IRequest<ProcessInvoiceMetadataResult>
    {
        public string BlobName { get; set; } = string.Empty;
    }
}
