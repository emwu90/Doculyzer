﻿using Doculyzer.Core.Models;

namespace Doculyzer.Core.Infrastructure.Repositories
{
    public interface IEvaluationMetricsRepository
    {
        Task SaveMetricsAsync(EvaluationMetrics metrics, CancellationToken cancellationToken = default);

        Task UpdateUserFeedbackAsync(string id, bool feedback, CancellationToken cancellationToken = default);
    }
}
