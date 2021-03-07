using FluentAssertions;
using InvoiceProcessor.Entities;
using InvoiceProcessor.Infrastructure;
using InvoiceProcessor.Tests.Unit.TestCommon;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceProcessor.Tests.Unit
{
    public class InvoiceWorkitemEventHandlerShould
    {
        private AsyncCollector<AcmeOrderEntry> _mockCollector;
        private ListLogger _testLogger;
        private List<AcmeOrder> _mockParsedOrders;
        private Mock<IAcmeInvoiceParser> _mockParser;
        private Mock<IBinder> _mockBlobBinder;

        public InvoiceWorkitemEventHandlerShould()
        {
            _testLogger = TestFactory.CreateLogger(LoggerTypes.List) as ListLogger;
            _mockCollector = new AsyncCollector<AcmeOrderEntry>();
            _mockParsedOrders = new List<AcmeOrder>();
            _mockParser = new Mock<IAcmeInvoiceParser>();
            _mockBlobBinder = new Mock<IBinder>();
        }

        delegate void TryParseCallback(Stream s, ILogger l, out List<AcmeOrder> o);

        [Fact]
        public async Task IgnoreEmptyMessagesFromServiceBus()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder>();
                }))
                .Returns(true);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;



            await InvoiceWorkitemEventHandler.Run(
                string.Empty,
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().Contain(l => l.Contains("Empty Invoice Workitem."));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Never);
        }

        [Fact]
        public async Task RejectInvalidMessagesFromServiceBus()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder>();
                }))
                .Returns(true);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;

            await InvoiceWorkitemEventHandler.Run(
                "Invalid work item",
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().NotContain(l => l.Contains("Empty Invoice Workitem."));
            logs.Should().Contain(l => l.Contains("Invalid Invoice Workitem."));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Never);
        }

        [Fact]
        public async Task RejectInvoiceFileNotStoredInAzureBlob()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder>();
                }))
                .Returns(true);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;

            var workitem = new InvoiceWorkitemMessage {
                CorrelationId = "TEST",
                TransactionId = Guid.NewGuid(),
                DataLocationType = DataLocationType.AwsS3,
                DataLocation = "path-to-s3-blob",
                UserEmail = "test@example.com",
            };

            var workItemJson = JsonSerializer.Serialize(workitem, new JsonSerializerOptions {
                WriteIndented = false
            });



            await InvoiceWorkitemEventHandler.Run(
                workItemJson,
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().NotContain(l => l.Contains("Empty Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Invalid Invoice Workitem."));
            logs.Should().Contain(l => l.Contains("Unsupported data location type"));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Never);
        }

        [Fact]
        public async Task RejectWorkItemWithEmptyBlobLink()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder>();
                }))
                .Returns(true);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;

            var workitem = new InvoiceWorkitemMessage {
                CorrelationId = "TEST",
                TransactionId = Guid.NewGuid(),
                DataLocationType = DataLocationType.AzureBlob,
                DataLocation = string.Empty,
                UserEmail = "test@example.com",
            };

            var workItemJson = JsonSerializer.Serialize(workitem, new JsonSerializerOptions {
                WriteIndented = false
            });



            await InvoiceWorkitemEventHandler.Run(
                workItemJson,
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().NotContain(l => l.Contains("Empty Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Invalid Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Unsupported data location type"));
            logs.Should().Contain(l => l.Contains("Empty data location."));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Never);
        }

        [Fact]
        public async Task RejectWorkItemThatFailedToGetParsed()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder>();
                }))
                .Returns(false);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;

            var workitem = new InvoiceWorkitemMessage {
                CorrelationId = "TEST",
                TransactionId = Guid.NewGuid(),
                DataLocationType = DataLocationType.AzureBlob,
                DataLocation = "azure/blob/location",
                UserEmail = "test@example.com",
            };

            var workItemJson = JsonSerializer.Serialize(workitem, new JsonSerializerOptions {
                WriteIndented = false
            });



            await InvoiceWorkitemEventHandler.Run(
                workItemJson,
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().NotContain(l => l.Contains("Empty Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Invalid Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Unsupported data location type"));
            logs.Should().NotContain(l => l.Contains("Empty data location."));
            logs.Should().Contain(l => l.Contains("Failed to parse invoice."));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Once);
        }

        [Fact]
        public async Task ProcessValidWorkItem()
        {
            _mockParser
                .Setup(x => x.TryParse(
                    It.IsAny<Stream>(),
                    _testLogger,
                    out _mockParsedOrders))
                .Callback(new TryParseCallback((Stream s, ILogger l, out List<AcmeOrder> o) => {
                    o = new List<AcmeOrder> {
                        new AcmeOrder{
                            OrderNumber = "One",
                        },
                        new AcmeOrder{
                            OrderNumber = "Two",
                        },
                    };
                }))
                .Returns(true);

            _mockBlobBinder
                .Setup(x => x.BindAsync<Stream>(
                    It.IsAny<BlobAttribute>(),
                    default))
                .Returns(Task.FromResult<Stream>(null));

            var sut = new InvoiceWorkitemEventHandler();
            InvoiceWorkitemEventHandler.InvoiceParser = _mockParser.Object;

            var workitem = new InvoiceWorkitemMessage {
                CorrelationId = "TEST",
                TransactionId = Guid.NewGuid(),
                DataLocationType = DataLocationType.AzureBlob,
                DataLocation = "azure/blob/location",
                UserEmail = "test@example.com",
            };

            var workItemJson = JsonSerializer.Serialize(workitem, new JsonSerializerOptions {
                WriteIndented = false
            });



            await InvoiceWorkitemEventHandler.Run(
                workItemJson,
                _mockBlobBinder.Object,
                _mockCollector,
                _testLogger);

            var logs = _testLogger.Logs;



            logs.Should().NotBeNull();
            logs.Should().NotBeEmpty();
            logs.Should().NotContain(l => l.Contains("Empty Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Invalid Invoice Workitem."));
            logs.Should().NotContain(l => l.Contains("Unsupported data location type"));
            logs.Should().NotContain(l => l.Contains("Empty data location."));
            logs.Should().NotContain(l => l.Contains("Failed to parse invoice."));
            _mockBlobBinder.Verify(b => b.BindAsync<Stream>(It.IsAny<BlobAttribute>(), default), Times.Once);

            _mockCollector.Items.Should().NotBeNullOrEmpty();
            _mockCollector.Items.Should().HaveCount(2);

            _mockCollector.Items.Should().OnlyContain(i => i.PartitionKey.Equals("test@example.com"));
            _mockCollector.Items.Should().Contain(i => i.RowKey.Equals("One"));
            _mockCollector.Items.Should().Contain(i => i.RowKey.Equals("Two"));
        }
    }
}
