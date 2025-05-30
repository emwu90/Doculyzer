using Doculyzer.Core.Models;

namespace Doculyzer.Request
{
    public class DocumentQueryResult
    {
        public string? Answer { get; set; }

        public List<Invoice> RelevantInvoices { get; set; } = new();

        public bool IsSuccessful { get; set; } = false;

        public string? ErrorMessage { get; set; }
    }
}
