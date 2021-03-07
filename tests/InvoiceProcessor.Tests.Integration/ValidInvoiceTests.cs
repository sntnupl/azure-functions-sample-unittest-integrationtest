using InvoiceProcessor.Entities;
using InvoiceProcessor.Tests.Integration.TestCommon;
using InvoiceProcessor.Tests.Integration.Utility;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace InvoiceProcessor.Tests.Integration
{
    public class ValidInvoiceTests: IClassFixture<ValidInvoiceTests.TestFixture>
    {
        private readonly TestFixture _testFixture;
        private readonly ITestOutputHelper _output;

        public ValidInvoiceTests(TestFixture testFixture, ITestOutputHelper output)
        {
            _testFixture = testFixture;
            _output = output;
        }


        [Fact]
        public async Task ValidInvoiceUploaded_GetsParsedAndSavedToTableStorage()
        {
            string blobPath = await _testFixture.UploadSampleValidInvoice();

            var resolver = new RandomNameResolver();
            IHost host = new HostBuilder()
                .ConfigureWebJobs()
                .ConfigureDefaultTestHost<InvoiceWorkitemEventHandler>(webjobsBuilder => {
                    webjobsBuilder.AddAzureStorage();
                    webjobsBuilder.AddServiceBus();
                })
                .ConfigureServices(services => {
                    services.AddSingleton<INameResolver>(resolver);
                })
                .Build();

            using (host) {
                await host.StartAsync();

                await _testFixture.SendInvoiceWorkitemEvent(blobPath);

                List<AcmeOrderEntry> parsedOrders = await _testFixture.WaitForTableUpdateAndGetParsedOrders();

                _output.WriteLine($"Found {parsedOrders.Count} invoices.");
                Assert.True(parsedOrders.Count == 2);
            }

        }

        public class TestFixture : EndToEndTestFixture
        {
            private const string SUT_OUTPUT_DIR = @"../../../../../src/InvoiceProcessor/bin/Debug/netcoreapp3.1";
            
            public TestFixture(IMessageSink sink)
                : base(sink, SUT_OUTPUT_DIR)
            {
            }
        }
    }
}
