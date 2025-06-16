using Doculyzer.Core.Models;
using Microsoft.Azure.Cosmos;

namespace Doculyzer.Core.Infrastructure.Repositories
{
    public class EvaluationMetricsRepository : IEvaluationMetricsRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;

        public EvaluationMetricsRepository(CosmosClient cosmosClient, string databaseName, string containerName)
        {
            _cosmosClient = cosmosClient;
            _container = _cosmosClient.GetDatabase(databaseName).GetContainer(containerName);
        }

        public async Task SaveMetricsAsync(EvaluationMetrics metrics, CancellationToken cancellationToken = default)
        {
            await _container.CreateItemAsync(metrics, new PartitionKey(metrics.Id), cancellationToken: cancellationToken);
        }
    }
}
