using Doculyzer.Core.Models;

namespace Doculyzer.Request
{
    public class ProcessInvoiceMetadataResult
    {
        public bool Success { get; set; } = false;

        public string? Message { get; set; }

        public Invoice? InvoiceData { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
