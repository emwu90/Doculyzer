namespace Doculyzer.Core.Models
{
    public class InvoiceLineItemSearchDocument
    {
        public string ProductName { get; set; } = string.Empty;

        public string ProductCode { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string Description { get; set; } = string.Empty;
    }
}
