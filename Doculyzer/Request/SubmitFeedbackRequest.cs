using Doculyzer.Core.Mediator;

namespace Doculyzer.Request
{
    public class SubmitFeedbackRequest : IRequest<SubmitFeedbackResult>
    {
        public required string ResponseId { get; set; }

        public required bool IsSatisfactory { get; set; }
    }
}
