namespace Doculyzer.Core.Models
{
    public class Invoice
    {
        public string BlobName { get; set; } = string.Empty;

        public string InvoiceNumber { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public string VendorName { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public List<InvoiceLineItem> LineItems { get; set; } = new();

        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
