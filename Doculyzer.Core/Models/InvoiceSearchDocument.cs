namespace Doculyzer.Core.Models
{
    public class InvoiceSearchDocument
    {
        public string Id { get; set; } = string.Empty;

        public string BlobName { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public List<InvoiceLineItemSearchDocument> LineItems { get; set; } = new();
    }
}
