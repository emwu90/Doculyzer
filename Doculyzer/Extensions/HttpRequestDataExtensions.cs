using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace Doculyzer.Extensions
{
    public static class HttpRequestDataExtensions
    {
        public static async Task<HttpResponseData> CreateJsonResponseAsync<T>(
            this HttpRequestData request, HttpStatusCode statusCode, T content)
        {
            var response = request.CreateResponse();
            response.StatusCode = statusCode;

            await response.WriteAsJsonAsync(content);

            return response;
        }
    }
}
