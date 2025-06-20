using Doculyzer.Core.Mediator;
using Doculyzer.Request;
using Microsoft.Extensions.Logging;
using Moq;

namespace Doculyzer.Tests;

[TestClass]
public class ProcessInvoiceMetadataTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private Mock<ILogger<ProcessInvoiceMetadata>> _loggerMock = null!;
    private ProcessInvoiceMetadata _function = null!;

    [TestInitialize]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<ProcessInvoiceMetadata>>();
        _function = new ProcessInvoiceMetadata(_mediatorMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task ProcessInvoiceBlob_DoesNotProcessNonPdfFiles()
    {
        // Arrange
        var blobName = "test.txt";
        var blobStream = new MemoryStream();

        // Act
        await _function.ProcessInvoiceBlob(blobStream, blobName, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.SendAsync(It.IsAny<ProcessInvoiceMetadataRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task ProcessInvoiceBlob_ProcessesPdfFiles()
    {
        // Arrange
        var blobName = "invoice.pdf";
        var blobStream = new MemoryStream();
        var request = new ProcessInvoiceMetadataRequest { BlobName = blobName };
        var expectedResult = new ProcessInvoiceMetadataResult();

        _mediatorMock
            .Setup(m => m.SendAsync(It.IsAny<ProcessInvoiceMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        await _function.ProcessInvoiceBlob(blobStream, blobName, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(m => m.SendAsync(It.Is<ProcessInvoiceMetadataRequest>(r => r.BlobName == blobName), It.IsAny<CancellationToken>()), Times.Once);
    }
}