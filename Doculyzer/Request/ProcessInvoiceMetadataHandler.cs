using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Doculyzer.Core.Services;
using Microsoft.Extensions.Logging;

namespace Doculyzer.Request
{
    public class ProcessInvoiceMetadataHandler : IRequestHandler<ProcessInvoiceMetadataRequest, ProcessInvoiceMetadataResult>
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IInvoiceSearchRepository _searchRepository;
        private readonly IInvoiceAnalysisService _analysisService;
        private readonly ILogger<ProcessInvoiceMetadataHandler> _logger;

        public ProcessInvoiceMetadataHandler(
            IInvoiceRepository invoiceRepository,
            IInvoiceSearchRepository searchRepository,
            IInvoiceAnalysisService analysisService,
            ILogger<ProcessInvoiceMetadataHandler> logger)
        {
            _invoiceRepository = invoiceRepository;
            _searchRepository = searchRepository;
            _analysisService = analysisService;
            _logger = logger;
        }

        public async Task<ProcessInvoiceMetadataResult> HandleAsync(ProcessInvoiceMetadataRequest request, CancellationToken cancellationToken = default)
        {

            try
            {
                if (await _invoiceRepository.IsProcessedBlobAsync(request.BlobName, cancellationToken))
                {
                    return new ProcessInvoiceMetadataResult
                    {
                        Success = true,
                        Message = "Invoice has already been processed."
                    };
                }

                // Step 1: Get the PDF stream from blob storage
                var pdfStream = await _invoiceRepository.GetInvoiceBlobStreamAsync(request.BlobName, cancellationToken);

                // Step 2: Extract invoice data using Document Intelligence
                var invoiceData = await _analysisService.ExtractInvoiceDataFromPdfAsync(pdfStream, request.BlobName, cancellationToken);

                // Step 3: Update blob metadata with extracted information
                var metadata = new Dictionary<string, string>
                {
                    ["InvoiceNumber"] = invoiceData.InvoiceNumber,
                    ["InvoiceDate"] = invoiceData.InvoiceDate.ToString("yyyy-MM-dd"),
                    ["CustomerName"] = invoiceData.CustomerName,
                    ["CustomerId"] = invoiceData.CustomerId,
                    ["TotalAmount"] = invoiceData.TotalAmount.ToString("F2"),
                    ["Currency"] = invoiceData.Currency,
                    ["ProcessedDate"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    ["Status"] = "Processed"
                };

                await _invoiceRepository.UpdateInvoiceMetadataAsync(request.BlobName, metadata, cancellationToken);

                // Step 4: Index the invoice in Azure AI Search
                await _searchRepository.IndexInvoiceAsync(invoiceData, cancellationToken);

                return new ProcessInvoiceMetadataResult
                {
                    Success = true,
                    InvoiceData = invoiceData,
                    Message = "Invoice processed successfully"
                };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error processing invoice metadata for blob {BlobName}: {Message}", request.BlobName, exception.Message);
                return new ProcessInvoiceMetadataResult
                {
                    ErrorMessage = exception.Message,
                    Success = false
                };
            }
        }
    }
}
