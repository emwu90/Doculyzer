using Azure;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Models;

namespace Doculyzer.Core.Services
{
    public class InvoiceAnalysisService : IInvoiceAnalysisService
    {
        private readonly IAzureOpenAIService _openAIService;
        private readonly IAzureServiceFactory _serviceFactory;

        public InvoiceAnalysisService(IAzureOpenAIService openAIService, IAzureServiceFactory serviceFactory)
        {
            _openAIService = openAIService;
            _serviceFactory = serviceFactory;
        }

        public async Task<string> AnalyzeInvoicesForQueryAsync(List<Invoice> invoices, string query, CancellationToken cancellationToken = default)
        {
            return await _openAIService.GenerateAnswerAsync(query, invoices, cancellationToken);
        }

        public async Task<Invoice> ExtractInvoiceDataFromPdfAsync(Stream pdfStream, string blobName, CancellationToken cancellationToken = default)
        {
            var documentAnalysisClient = _serviceFactory.CreateDocumentAnalysisClient();

            var operation = await documentAnalysisClient.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-invoice",
                pdfStream,
                cancellationToken: cancellationToken);

            var result = operation.Value;
            var invoice = new Invoice { BlobName = blobName };

            if (result.Documents.Count > 0)
            {
                var document = result.Documents[0];

                // Extract invoice fields
                if (document.Fields.TryGetValue("InvoiceId", out var invoiceId))
                    invoice.InvoiceNumber = invoiceId.Value.AsString();

                if (document.Fields.TryGetValue("InvoiceDate", out var invoiceDate))
                    invoice.InvoiceDate = invoiceDate.Value.AsDate().DateTime;

                if (document.Fields.TryGetValue("VendorName", out var vendorName))
                    invoice.VendorName = vendorName.Value.AsString();

                if (document.Fields.TryGetValue("CustomerName", out var customerName))
                    invoice.CustomerName = customerName.Value.AsString();

                if (document.Fields.TryGetValue("CustomerId", out var customerId))
                    invoice.CustomerId = customerId.Value.AsString();

                if (document.Fields.TryGetValue("InvoiceTotal", out var invoiceTotal))
                {
                    var invoiceTotalCurrency = invoiceTotal.Value.AsCurrency();
                    invoice.TotalAmount = (decimal)invoiceTotalCurrency.Amount;
                    invoice.Currency = invoiceTotalCurrency.Code;
                }

                // Extract line items
                if (document.Fields.TryGetValue("Items", out var items))
                {
                    foreach (var item in items.Value.AsList())
                    {
                        var lineItem = new InvoiceLineItem();
                        var itemFields = item.Value.AsDictionary();

                        if (itemFields.TryGetValue("Description", out var description))
                            lineItem.ProductName = description.Value.AsString();

                        if (itemFields.TryGetValue("Quantity", out var quantity))
                            lineItem.Quantity = (int)quantity.Value.AsDouble();

                        if (itemFields.TryGetValue("UnitPrice", out var unitPrice))
                            lineItem.UnitPrice = (decimal)unitPrice.Value.AsCurrency().Amount;

                        if (itemFields.TryGetValue("Tax", out var unitPriceWithTax))
                            lineItem.UnitPriceWithTax = (decimal)unitPriceWithTax.Value.AsCurrency().Amount;

                        if (itemFields.TryGetValue("Amount", out var amount))
                            lineItem.TotalPrice = (decimal)amount.Value.AsCurrency().Amount;

                        invoice.LineItems.Add(lineItem);
                    }
                }
            }

            return invoice;
        }
    }
}
