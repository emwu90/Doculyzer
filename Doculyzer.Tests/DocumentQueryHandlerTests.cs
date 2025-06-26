using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Models;
using Doculyzer.Core.Services;
using Doculyzer.Request;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doculyzer.Tests;

[TestClass]
public class DocumentQueryHandlerTests
{
    private Mock<IAzureOpenAIService> _openAIServiceMock = null!;
    private Mock<IInvoiceSearchRepository> _searchRepositoryMock = null!;
    private Mock<IInvoiceAnalysisService> _analysisServiceMock = null!;
    private Mock<ILogger<DocumentQueryHandler>> _loggerMock = null!;
    private DocumentQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _openAIServiceMock = new Mock<IAzureOpenAIService>();
        _searchRepositoryMock = new Mock<IInvoiceSearchRepository>();
        _analysisServiceMock = new Mock<IInvoiceAnalysisService>();
        _loggerMock = new Mock<ILogger<DocumentQueryHandler>>();

        _handler = new DocumentQueryHandler(
            _openAIServiceMock.Object,
            _searchRepositoryMock.Object,
            _analysisServiceMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task HandleAsync_ReturnsSuccessfulResult_WhenQueryIsValid()
    {
        // Arrange
        var request = new DocumentQueryRequest { Prompt = "Find all invoices" };
        var queryIntent = new QueryIntent { QueryType = QueryType.General, SearchTerm = "invoices" };
        var invoices = new List<Invoice> { new Invoice { InvoiceNumber = "12345" } };
        var responseResult = new ResponseResult { ResponseText = "Invoices found", ResponseId = "e5881ef1" };

        _openAIServiceMock
            .Setup(s => s.ParseQueryIntentAsync(request.Prompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryIntent);

        _searchRepositoryMock
            .Setup(r => r.SearchInvoicesAsync(queryIntent.SearchTerm!, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoices);

        _analysisServiceMock
            .Setup(a => a.AnalyzeInvoicesForQueryAsync(invoices, request.Prompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseResult);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(responseResult.ResponseText, result.Answer);
        CollectionAssert.AreEqual(invoices, result.RelevantInvoices);
    }

    [TestMethod]
    public async Task HandleAsync_ReturnsErrorResult_WhenQueryIntentFails()
    {
        // Arrange
        var request = new DocumentQueryRequest { Prompt = "Invalid query" };

        _openAIServiceMock
            .Setup(s => s.ParseQueryIntentAsync(request.Prompt, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Query intent could not be parsed."));

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual("Query intent could not be parsed.", result.ErrorMessage);
    }

    [TestMethod]
    public async Task HandleAsync_ReturnsErrorResult_WhenSearchFails()
    {
        // Arrange
        var request = new DocumentQueryRequest { Prompt = "Find all invoices" };
        var queryIntent = new QueryIntent { QueryType = QueryType.General, SearchTerm = "invoices" };

        _openAIServiceMock
            .Setup(s => s.ParseQueryIntentAsync(request.Prompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryIntent);

        _searchRepositoryMock
            .Setup(r => r.SearchInvoicesAsync(queryIntent.SearchTerm!, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Search failed"));

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.IsSuccessful);
        Assert.AreEqual("Search failed", result.ErrorMessage);
    }
}