using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Security.Claims;
using System.Text;

namespace Doculyzer.Tests.Common
{
    public sealed class FakeHttpRequestData : HttpRequestData
    {
        private readonly MemoryStream _bodyStream;
        private readonly FunctionContext _functionContext;

        public FakeHttpRequestData(FunctionContext functionContext, string bodyJson, string method)
            : base(functionContext)
        {
            _functionContext = functionContext;
            _bodyStream = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));
            Method = method;
        }

        public override Stream Body => _bodyStream;

        public override HttpHeadersCollection Headers { get; } = [];

        public override IReadOnlyCollection<IHttpCookie> Cookies => [];

        public override Uri Url => new("https://localhost");

        public override IEnumerable<ClaimsIdentity> Identities => [];

        public override string Method { get; }

        public override HttpResponseData CreateResponse()
        {
            return new FakeHttpResponseData(_functionContext);
        }
    }
}
