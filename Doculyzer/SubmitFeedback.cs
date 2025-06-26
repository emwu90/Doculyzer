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

public class SubmitFeedback
{
    private readonly IMediator _mediator;
    private readonly ILogger<DoculyzerAgent> _logger;

    public SubmitFeedback(
        IMediator mediator,
        ILogger<DoculyzerAgent> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [Function(nameof(SubmitFeedback))]
    [OpenApiOperation(operationId: "SubmitFeedback", tags: new[] { "Feedback" }, Summary = "Submit user feedback for an AI response", Description = "Allows a user to submit feedback (satisfactory or not) for a specific AI response, identified by its EvaluationId. This feedback is stored for monitoring and future improvements.")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(SubmitFeedbackRequest), Required = true, Description = "The feedback request containing the EvaluationId of the response and a boolean indicating if the response was satisfactory.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(SubmitFeedbackResult), Summary = "Feedback submitted successfully", Description = "Indicates that the feedback was successfully recorded.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Summary = "Invalid feedback request", Description = "Returned when the feedback request is missing required fields or is otherwise invalid.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(DocumentQueryResult), Summary = "Server error", Description = "Returned when an error occurs while processing the feedback.")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "feedback")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
        var feedbackRequest = JsonSerializer.Deserialize<SubmitFeedbackRequest>(requestBody);

        if (feedbackRequest == null || string.IsNullOrEmpty(feedbackRequest.ResponseId))
        {
            var badRequestResponse = request.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteStringAsync("Invalid feedback request.");
            return badRequestResponse;
        }

        _logger.LogInformation("Started adding user feedback to metrics.");

        var result = await _mediator.SendAsync(feedbackRequest, cancellationToken);
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            return await request.CreateJsonResponseAsync(HttpStatusCode.InternalServerError, new DocumentQueryResult { ErrorMessage = "Cannot save user feedback." });
        }

        return await request.CreateJsonResponseAsync(HttpStatusCode.OK, result);
    }
}