using Doculyzer.Core.Mediator;

namespace Doculyzer.Request
{
    public class DocumentQueryRequest : IRequest<DocumentQueryResult>
    {
        public required string Prompt { get; set; }
    }
}
