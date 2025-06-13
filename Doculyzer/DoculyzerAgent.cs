using Doculyzer.Core.Mediator;
using Doculyzer.Extensions;
using Doculyzer.Request;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Doculyzer;

public class DoculyzerAgent
{
    private readonly IMediator _mediator;
    private readonly ILogger<DoculyzerAgent> _logger;

    public DoculyzerAgent(
        IMediator mediator,
        ILogger<DoculyzerAgent> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function(nameof(DoculyzerAgent))]
    [OpenApiOperation(operationId: "Run", tags: new[] { "DoculyzerAgent" }, Summary = "Query documents", Description = "Queries documents using a natural language prompt.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(DocumentQueryRequest), Required = true, Description = "The query request containing the prompt.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(DocumentQueryResult), Summary = "Successful response", Description = "The result of the document query.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Invalid request", Description = "The request was invalid.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.InternalServerError, Summary = "Server error", Description = "An error occurred while processing the request.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "agent")] HttpRequestData request, CancellationToken cancellationToken)
    {
        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        var queryRequest = JsonSerializer.Deserialize<DocumentQueryRequest>(requestBody);

        if (queryRequest == null || string.IsNullOrEmpty(queryRequest.Prompt))
        {
            var badRequestResponse = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Invalid request. Prompt is required.");
            return badRequestResponse;
        }

        _logger.LogInformation("Processing document query request for prompt: {Prompt}", queryRequest.Prompt);

        var result = await _mediator.SendAsync(queryRequest, cancellationToken);
        if (!result.IsSuccessful)
        {
            return await request.CreateJsonResponseAsync(HttpStatusCode.InternalServerError, new DocumentQueryResult { ErrorMessage = result.ErrorMessage });
        }

        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result);
    }
}
