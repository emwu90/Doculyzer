using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Doculyzer.Core.Infrastructure.Factories;
using Doculyzer.Core.Models;

namespace Doculyzer.Core.Infrastructure.Repositories
{
    public class InvoiceSearchRepository : IInvoiceSearchRepository
    {
        private readonly IAzureServiceFactory _serviceFactory;

        public InvoiceSearchRepository(IAzureServiceFactory serviceFactory)
        {
            _serviceFactory = serviceFactory;
        }

        public async Task<List<Invoice>> SearchInvoicesAsync(string query, Dictionary<string, object>? filters = null, CancellationToken cancellationToken = default)
        {
            var searchClient = _serviceFactory.CreateSearchClient();
            var searchOptions = new SearchOptions
            {
                Size = 1000,
                IncludeTotalCount = true
            };

            if (filters != null)
            {
                var filterExpressions = filters.Select(kvp => $"{kvp.Key} eq '{kvp.Value}'");
                searchOptions.Filter = string.Join(" and ", filterExpressions);
            }

            var response = await searchClient.SearchAsync<InvoiceSearchDocument>(query, searchOptions, cancellationToken);

            var invoices = new List<Invoice>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                invoices.Add(MapSearchDocumentToInvoiceData(result.Document));
            }

            return invoices;
        }

        public async Task<List<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            var searchClient = _serviceFactory.CreateSearchClient();
            var searchOptions = new SearchOptions
            {
                Filter = $"InvoiceDate ge {startDate:yyyy-MM-ddThh:mm:ssZ} and InvoiceDate le {endDate:yyyy-MM-ddThh:mm:ssZ}",
                Size = 1000
            };

            var response = await searchClient.SearchAsync<InvoiceSearchDocument>("*", searchOptions, cancellationToken);

            var invoices = new List<Invoice>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                invoices.Add(MapSearchDocumentToInvoiceData(result.Document));
            }

            return invoices;
        }

        public async Task<List<Invoice>> GetInvoicesByCustomerAsync(string customerName, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
        {
            var searchClient = _serviceFactory.CreateSearchClient();

            var filter = $"CustomerName eq '{customerName}'";
            if (startDate.HasValue)
                filter += $" and InvoiceDate ge {startDate.Value:yyyy-MM-ddThh:mm:ssZ}";
            if (endDate.HasValue)
                filter += $" and InvoiceDate le {endDate.Value:yyyy-MM-ddThh:mm:ssZ}";

            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = 1000
            };

            var response = await searchClient.SearchAsync<InvoiceSearchDocument>("*", searchOptions, cancellationToken);

            var invoices = new List<Invoice>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                invoices.Add(MapSearchDocumentToInvoiceData(result.Document));
            }

            return invoices;
        }

        public async Task IndexInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            var searchClient = _serviceFactory.CreateSearchClient();
            var document = MapInvoiceDataToSearchDocument(invoice);

            var batch = IndexDocumentsBatch.Create(IndexDocumentsAction.MergeOrUpload(document));
            await searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
        }

        private Invoice MapSearchDocumentToInvoiceData(InvoiceSearchDocument document)
        {
            return new Invoice
            {
                BlobName = document.BlobName,
                InvoiceNumber = document.InvoiceNumber,
                InvoiceDate = document.InvoiceDate,
                CustomerName = document.CustomerName,
                CustomerId = document.CustomerId,
                TotalAmount = document.TotalAmount,
                Currency = document.Currency,
                LineItems = document.LineItems?.Select(li => new InvoiceLineItem
                {
                    ProductName = li.ProductName,
                    ProductCode = li.ProductCode,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TotalPrice = li.TotalPrice,
                    Description = li.Description
                }).ToList() ?? new List<InvoiceLineItem>()
            };
        }

        private InvoiceSearchDocument MapInvoiceDataToSearchDocument(Invoice invoice)
        {
            return new InvoiceSearchDocument
            {
                Id = invoice.BlobName.Replace('/', '_').Replace('.', '_'),
                BlobName = invoice.BlobName,
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                CustomerName = invoice.CustomerName,
                CustomerId = invoice.CustomerId,
                TotalAmount = invoice.TotalAmount,
                Currency = invoice.Currency,
                LineItems = invoice.LineItems.Select(li => new InvoiceLineItemSearchDocument
                {
                    ProductName = li.ProductName,
                    ProductCode = li.ProductCode,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    TotalPrice = li.TotalPrice,
                    Description = li.Description
                }).ToList()
            };
        }
    }
}
