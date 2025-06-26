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

namespace Doculyzer.Tests
{
    [TestClass]
    public class SubmitFeedbackTests
    {
        private Mock<IMediator> _mediatorMock = null!;
        private Mock<ILogger<DoculyzerAgent>> _loggerMock = null!;
        private SubmitFeedback _function = null!;

        [TestInitialize]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<DoculyzerAgent>>();
            _function = new SubmitFeedback(_mediatorMock.Object, _loggerMock.Object);
        }

        [TestMethod]
        public async Task Run_ReturnsOk_WhenFeedbackIsValid()
        {
            // Arrange
            var feedbackRequest = new SubmitFeedbackRequest { ResponseId = "e5881ef1", IsSatisfactory = true };
            var expectedResult = new SubmitFeedbackResult { Success = true };

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<SubmitFeedbackRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var request = CreateHttpRequest(feedbackRequest);

            // Act
            var response = await _function.Run(request, CancellationToken.None);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            var receivedObject = GetBodyObjectFromResponse<SubmitFeedbackResult>(response);
            Assert.IsTrue(receivedObject.Success);
            Assert.IsNull(receivedObject.ErrorMessage);
        }

        [TestMethod]
        public async Task Run_ReturnsBadRequest_WhenEvaluationIdIsMissing()
        {
            // Arrange
            var feedbackRequest = new SubmitFeedbackRequest { ResponseId = "", IsSatisfactory = true };
            var request = CreateHttpRequest(feedbackRequest);

            // Act
            var response = await _function.Run(request, CancellationToken.None);

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var receivedMessage = GetBodyMessageFromResponse(response);
            Assert.IsTrue(receivedMessage.Contains("Invalid feedback request"));
        }

        [TestMethod]
        public async Task Run_ReturnsInternalServerError_WhenHandlerReturnsError()
        {
            // Arrange
            var feedbackRequest = new SubmitFeedbackRequest { ResponseId = "abc123", IsSatisfactory = false };
            var expectedResult = new SubmitFeedbackResult { Success = false, ErrorMessage = "Failed to update feedback" };

            _mediatorMock
                .Setup(m => m.SendAsync(It.IsAny<SubmitFeedbackRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            var request = CreateHttpRequest(feedbackRequest);

            // Act
            var response = await _function.Run(request, CancellationToken.None);

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            var receivedObject = GetBodyObjectFromResponse<DocumentQueryResult>(response);
            Assert.AreEqual("Failed to update feedback", receivedObject.ErrorMessage);
        }

        private static HttpRequestData CreateHttpRequest(object body)
        {
            IOptions<WorkerOptions> workerOptions = Options.Create<WorkerOptions>(new WorkerOptions() { Serializer = new Azure.Core.Serialization.JsonObjectSerializer() });
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

        private static T GetBodyObjectFromResponse<T>(HttpResponseData response)
        {
            string body = string.Empty;
            using (var reader = new StreamReader(response.Body))
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                body = reader.ReadToEnd();
            }

            var result = JsonSerializer.Deserialize<T>(body);
            if (result == null)
            {
                throw new InvalidOperationException($"Failed to deserialize response body into {typeof(T).Name}.");
            }

            return result;
        }
    }
}
