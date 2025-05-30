namespace Doculyzer.Core.Models
{
    public class QueryIntent
    {
        public QueryType QueryType { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? CustomerName { get; set; }

        public string? SearchTerm { get; set; }
    }
}
