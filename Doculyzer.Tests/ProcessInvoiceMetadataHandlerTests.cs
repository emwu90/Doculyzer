using Doculyzer.Core.Infrastructure.Repositories;
using Doculyzer.Core.Models;
using Doculyzer.Core.Services;
using Doculyzer.Request;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doculyzer.Tests;

[TestClass]
public class ProcessInvoiceMetadataHandlerTests
{
    private Mock<IInvoiceRepository> _invoiceRepositoryMock = null!;
    private Mock<IInvoiceSearchRepository> _searchRepositoryMock = null!;
    private Mock<IInvoiceAnalysisService> _analysisServiceMock = null!;
    private Mock<ILogger<ProcessInvoiceMetadataHandler>> _loggerMock = null!;
    private ProcessInvoiceMetadataHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _searchRepositoryMock = new Mock<IInvoiceSearchRepository>();
        _analysisServiceMock = new Mock<IInvoiceAnalysisService>();
        _loggerMock = new Mock<ILogger<ProcessInvoiceMetadataHandler>>();

        _handler = new ProcessInvoiceMetadataHandler(
            _invoiceRepositoryMock.Object,
            _searchRepositoryMock.Object,
            _analysisServiceMock.Object,
            _loggerMock.Object);
    }

    [TestMethod]
    public async Task HandleAsync_ReturnsAlreadyProcessedResult_WhenBlobIsAlreadyProcessed()
    {
        // Arrange
        var request = new ProcessInvoiceMetadataRequest { BlobName = "invoice.pdf" };

        _invoiceRepositoryMock
            .Setup(r => r.IsProcessedBlobAsync(request.BlobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Invoice has already been processed.", result.Message);
        _invoiceRepositoryMock.Verify(r => r.GetInvoiceBlobStreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task HandleAsync_ProcessesInvoiceSuccessfully_WhenBlobIsNotProcessed()
    {
        // Arrange
        var request = new ProcessInvoiceMetadataRequest { BlobName = "invoice.pdf" };
        var invoiceData = new Invoice
        {
            InvoiceNumber = "12345",
            InvoiceDate = DateTime.UtcNow,
            CustomerName = "John Doe",
            CustomerId = "C123",
            TotalAmount = 100.50m,
            Currency = "USD"
        };

        _invoiceRepositoryMock
            .Setup(r => r.IsProcessedBlobAsync(request.BlobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _invoiceRepositoryMock
            .Setup(r => r.GetInvoiceBlobStreamAsync(request.BlobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream());

        _analysisServiceMock
            .Setup(a => a.ExtractInvoiceDataFromPdfAsync(It.IsAny<Stream>(), request.BlobName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invoiceData);

        _invoiceRepositoryMock
            .Setup(r => r.UpdateInvoiceMetadataAsync(request.BlobName, It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _searchRepositoryMock
            .Setup(s => s.IndexInvoiceAsync(invoiceData, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Invoice processed successfully", result.Message);
        Assert.AreEqual(invoiceData, result.InvoiceData);
    }

    [TestMethod]
    public async Task HandleAsync_ReturnsErrorResult_WhenExceptionOccurs()
    {
        // Arrange
        var request = new ProcessInvoiceMetadataRequest { BlobName = "invoice.pdf" };

        _invoiceRepositoryMock
            .Setup(r => r.IsProcessedBlobAsync(request.BlobName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Blob not found"));

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Blob not found", result.ErrorMessage);
    }
}