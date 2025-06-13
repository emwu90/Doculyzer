using Azure.Core.Serialization;
using Doculyzer.Core.Mediator;
using Doculyzer.Request;
using Doculyzer.Tests.Common;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text.Json;

namespace Doculyzer.Tests;

[TestClass]
public class DoculyzerAgentTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private Mock<ILogger<DoculyzerAgent>> _loggerMock = null!;
    private DoculyzerAgent _function = null!;

    [TestInitialize]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<DoculyzerAgent>>();
        _function = new DoculyzerAgent(_mediatorMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task Run_ReturnsOk_WhenPromptIsValid()
    {
        // Arrange
        var queryRequest = new DocumentQueryRequest { Prompt = "Find all invoices" };
        var expectedResult = new DocumentQueryResult { Answer = "Invoices found", IsSuccessful = true };

        _mediatorMock
            .Setup(m => m.SendAsync(It.IsAny<DocumentQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var request = CreateHttpRequest(queryRequest);

        // Act
        var response = await _function.Run(request, CancellationToken.None);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var receivedObject = GetBodyObjectFromResponse(response);
        Assert.AreEqual(expectedResult.Answer, receivedObject.Answer);
        Assert.AreEqual(expectedResult.IsSuccessful, receivedObject.IsSuccessful);
        Assert.AreEqual(expectedResult.ErrorMessage, receivedObject.ErrorMessage);
        CollectionAssert.AreEqual(expectedResult.RelevantInvoices, receivedObject.RelevantInvoices);
    }

    [TestMethod]
    public async Task Run_ReturnsBadRequest_WhenPromptIsMissing()
    {
        // Arrange
        var queryRequest = new DocumentQueryRequest { Prompt = string.Empty };
        var request = CreateHttpRequest(queryRequest);

        // Act
        var response = await _function.Run(request, CancellationToken.None);

        // Assert
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var receivedMessage = GetBodyMessageFromResponse(response);
        Assert.IsTrue(receivedMessage.Equals("Invalid request. Prompt is required."));
    }

    [TestMethod]
    public async Task Run_ReturnsInternalServerError_WhenResultIsNotSuccessful()
    {
        // Arrange
        var queryRequest = new DocumentQueryRequest { Prompt = "Find all invoices" };
        var expectedResult = new DocumentQueryResult { Answer = null, IsSuccessful = false, ErrorMessage = "An error occurred" };

        _mediatorMock
            .Setup(m => m.SendAsync(It.IsAny<DocumentQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var request = CreateHttpRequest(queryRequest);

        // Act
        var response = await _function.Run(request, CancellationToken.None);

        // Assert
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        var receivedObject = GetBodyObjectFromResponse(response);
        Assert.AreEqual(expectedResult.Answer, receivedObject.Answer);
        Assert.AreEqual(expectedResult.IsSuccessful, receivedObject.IsSuccessful);
        Assert.AreEqual(expectedResult.ErrorMessage, receivedObject.ErrorMessage);
        CollectionAssert.AreEqual(expectedResult.RelevantInvoices, receivedObject.RelevantInvoices);
    }

    private static HttpRequestData CreateHttpRequest(DocumentQueryRequest body)
    {
        IOptions<WorkerOptions> workerOptions = Options.Create<WorkerOptions>(new WorkerOptions() { Serializer = new JsonObjectSerializer() });
        var service = new ServiceCollection();
        service.AddSingleton<IOptions<WorkerOptions>>(workerOptions);

        var functionContext = new Mock<FunctionContext>();
        functionContext.Setup(m => m.InstanceServices).Returns(service.BuildServiceProvider());
        var json = JsonSerializer.Serialize(body);
        return new FakeHttpRequestData(functionContext.Object, json, "POST");
    }

    private static string GetBodyMessageFromResponse(HttpResponseData response)
    {
        string body = string.Empty;
        using (var reader = new StreamReader(response.Body))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            body = reader.ReadToEnd();
        }
        return body;
    }

    private static DocumentQueryResult GetBodyObjectFromResponse(HttpResponseData response)
    {
        string body = string.Empty;
        using (var reader = new StreamReader(response.Body))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            body = reader.ReadToEnd();
        }

        var result = JsonSerializer.Deserialize<DocumentQueryResult>(body);
        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize response body into DocumentQueryResult.");
        }

        return result;
    }
}
