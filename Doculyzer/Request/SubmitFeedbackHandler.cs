using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Mediator;
using Microsoft.Extensions.Logging;

namespace Doculyzer.Request
{
    public class SubmitFeedbackHandler : IRequestHandler<SubmitFeedbackRequest, SubmitFeedbackResult>
    {
        private readonly IEvaluationMetricsRepository _evaluationMetricsRepository;
        private readonly ILogger<SubmitFeedbackHandler> _logger;

        public SubmitFeedbackHandler(
            IEvaluationMetricsRepository evaluationMetricsRepository,
            ILogger<SubmitFeedbackHandler> logger)
        {
            _evaluationMetricsRepository = evaluationMetricsRepository;
            _logger = logger;
        }

        public async Task<SubmitFeedbackResult> HandleAsync(SubmitFeedbackRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                await _evaluationMetricsRepository.UpdateUserFeedbackAsync(request.ResponseId, request.IsSatisfactory, cancellationToken);
                return new SubmitFeedbackResult { Success = true };
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while adding user feedback: {Message}", exception.Message);
                return new SubmitFeedbackResult { Success = false, ErrorMessage = exception.Message };
            }
        }
    }
}
