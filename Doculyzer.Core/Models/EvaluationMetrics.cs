using System.Text.Json.Serialization;

namespace Doculyzer.Core.Models
{
    public class EvaluationMetrics
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string? Query { get; set; }

        public string? Response { get; set; }

        public double? Groundedness { get; set; } // Measures how well the response is based on provided data

        public double? Relevance { get; set; } // Measures how relevant the response is to the query

        public double Completeness { get; set; } // Measures how complete the response is in addressing the query

        public double Latency { get; set; } // Measures the time taken to generate the response

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
