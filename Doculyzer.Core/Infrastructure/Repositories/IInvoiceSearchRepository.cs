using Doculyzer.Core.Models;

namespace Doculyzer.Core.Infrastructure.Repositories
{
    public interface IInvoiceSearchRepository
    {
        Task<List<Invoice>> SearchInvoicesAsync(string query, Dictionary<string, object>? filters = null, CancellationToken cancellationToken = default);

        Task<List<Invoice>> GetInvoicesByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        Task<List<Invoice>> GetInvoicesByCustomerAsync(string customerId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        Task IndexInvoiceAsync(Invoice invoice, CancellationToken cancellationToken = default);
    }
}
