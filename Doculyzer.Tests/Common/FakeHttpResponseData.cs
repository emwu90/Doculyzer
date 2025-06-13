using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Doculyzer.Tests.Common
{
    public sealed class FakeHttpResponseData : HttpResponseData
    {
        public FakeHttpResponseData(FunctionContext context) : base(context)
        {
            Headers = new HttpHeadersCollection();
        }

        public override HttpStatusCode StatusCode { get; set; }

        public override HttpHeadersCollection Headers { get; set; }

        public override Stream Body { get; set; } = new MemoryStream();

        public override HttpCookies Cookies { get; }
    }
}
