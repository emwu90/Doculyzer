using Doculyzer.Core.Models;

namespace Doculyzer.Core.Services
{
    public interface IInvoiceAnalysisService
    {
        Task<ResponseResult> AnalyzeInvoicesForQueryAsync(List<Invoice> invoices, string query, CancellationToken cancellationToken = default);

        Task<Invoice> ExtractInvoiceDataFromPdfAsync(Stream pdfStream, string blobName, CancellationToken cancellationToken = default);
    }
}
