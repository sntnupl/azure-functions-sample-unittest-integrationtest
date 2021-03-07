using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InvoiceProcessor.Entities;
using InvoiceProcessor.Tests.Integration.Utility;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace InvoiceProcessor.Tests.Integration
{
    public class EndToEndTestFixture : IAsyncLifetime
    {
        private const string TEST_CONTAINER_PREFIX = "acme-e2etests";
        private const string INVOICE_TABLE = "AcmeOrders";
        private const string TOPIC_NAME = "workiteminvoice";
        private const string TEST_USER = "testuser@example.com";

        private readonly SettingsManager _settingsManager;
        private readonly IMessageSink _sink;

        public ServiceBusClient BusClient { get; set; }

        public CloudTableClient TableClient { get; set; }

        private BlobServiceClient BlobService { get; set; }

        private BlobContainerClient BlobContainer { get; set; }

        public EndToEndTestFixture(IMessageSink sink, string sutRootPath)
        {
            _sink = sink;
            _settingsManager = SettingsManager.Instance;

            UpdateEnvironmentVariables(Path.Combine(sutRootPath, "local.settings.json"));

            var connectionStringStorage = _settingsManager.GetSetting("StorageConnection");
            var connectionStringServiceBus = _settingsManager.GetSetting("ServiceBusConnection");

            var storageAccount = CloudStorageAccount.Parse(connectionStringStorage);
            BlobService = new BlobServiceClient(connectionStringStorage);

            BusClient = new ServiceBusClient(connectionStringServiceBus);
            TableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
        }

        public async Task InitializeAsync()
        {
            await DeleteTestContainers();
            await CreateTestBlobContainer();
            await RemoveInvoiceEntriesFromTestUser();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }


        public async Task<string> UploadSampleValidInvoice()
        {
            var localPath = "./testdata";
            var testFile = "sample-acme-invoice-file.txt";
            var localFilePath = Path.Combine(localPath, testFile);

            var blobClient = BlobContainer.GetBlobClient(testFile);

            using var uploadFileStream = File.OpenRead(localFilePath);
            await blobClient.UploadAsync(uploadFileStream, true);
            uploadFileStream.Close();

            _sink.OnMessage(new DiagnosticMessage($"Sample valid invoice file uploaded to: {blobClient.Uri}"));

            return $"{blobClient.BlobContainerName}/{blobClient.Name}";
        }

        public async Task SendInvoiceWorkitemEvent(string blobPath)
        {
            var sender = BusClient.CreateSender(TOPIC_NAME);
            var workitem = new InvoiceWorkitemMessage {
                CorrelationId = "TEST",
                TransactionId = Guid.NewGuid(),
                DataLocationType = DataLocationType.AzureBlob,
                DataLocation = blobPath,
                UserEmail = TEST_USER,
            };
            var workItemJson = JsonSerializer.Serialize(workitem, new JsonSerializerOptions {
                WriteIndented = false
            });

            await sender.SendMessageAsync(new ServiceBusMessage(workItemJson));
            _sink.OnMessage(new DiagnosticMessage("Sent Invoice Work event"));
        }

        public async Task<List<AcmeOrderEntry>> WaitForTableUpdateAndGetParsedOrders()
        {
            var partitionQuery = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TEST_USER);
            TableQuery composedQuery = new TableQuery();
            composedQuery.FilterString = partitionQuery;

            var invoiceTable = TableClient.GetTableReference(INVOICE_TABLE);
            if (!await invoiceTable.ExistsAsync()) {
                return default;
            }

            var entities = new List<AcmeOrderEntry>();
            DateTime start = DateTime.Now;
            int timeout = 60 * 1000;
            while (entities.Count < 1) {
                if (!Debugger.IsAttached &&
                    (DateTime.Now - start).TotalMilliseconds > timeout) {
                    throw new ApplicationException("Condition not reached within timeout.");
                }
                await Task.Delay(2 * 1000);

                var entitiesToFind = new TableQuery<AcmeOrderEntry>().Where(composedQuery.FilterString).Take(100);
                entities = invoiceTable.ExecuteQuery(entitiesToFind).ToList();
            }

            return entities;
        }





        private async Task DeleteTestContainers()
        {
            var deleteCount = 0;
            var testContainers = await GetTestContainers();
            while (testContainers.Count > 0) {
                foreach (var container in testContainers) {
                    var containerClient = BlobService.GetBlobContainerClient(container.Name);
                    try {
                        await containerClient.DeleteAsync();
                        ++deleteCount;
                    }
                    catch (Exception ex) {
                        Debug.WriteLine(ex.Message);
                        return;
                    }
                }

                testContainers = await GetTestContainers();
            }
            _sink.OnMessage(new DiagnosticMessage("Cleaned up test containers, {0} containers were deleted.", deleteCount));
        }

        private async Task CreateTestBlobContainer()
        {
            var containerName = $"{TEST_CONTAINER_PREFIX}-{Guid.NewGuid()}";
            BlobContainer = await BlobService.CreateBlobContainerAsync(containerName);
            _sink.OnMessage(new DiagnosticMessage($"Created new blob container: {containerName}"));
        }

        private async Task RemoveInvoiceEntriesFromTestUser()
        {
            var invoiceTable = TableClient.GetTableReference(INVOICE_TABLE);
            if (!await invoiceTable.ExistsAsync()) {
                await invoiceTable.CreateIfNotExistsAsync();
                _sink.OnMessage(new DiagnosticMessage($"Table {INVOICE_TABLE} created."));
            }
            else {
                var projectionQuery = new TableQuery()
                  .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, TEST_USER))
                  .Select(new[] { "RowKey" });

                var entities = invoiceTable.ExecuteQuery(projectionQuery).ToList();
                var offset = 0;
                while (offset < entities.Count) {
                    var batch = new TableBatchOperation();
                    var rows = entities.Skip(offset).Take(100).ToList();
                    foreach (var row in rows) {
                        batch.Delete(row);
                    }

                    invoiceTable.ExecuteBatch(batch);
                    offset += rows.Count;
                }
                _sink.OnMessage(new DiagnosticMessage("Removed any test existing entities"));
            }
        }

        private void UpdateEnvironmentVariables(string secretsPath)
        {
            var localSettings = File.ReadAllText(secretsPath);
            JObject settingValues = JObject.Parse(localSettings)["Values"] as JObject;
            foreach (var secret in settingValues) {
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(secret.Key))) {
                    Environment.SetEnvironmentVariable(secret.Key, (string)secret.Value, EnvironmentVariableTarget.Process);
                }
            }
        }

        private async Task<List<BlobContainerItem>> GetTestContainers(int segmentSize = 100)
        {
            var result = new List<BlobContainerItem>();

            try {
                // Call the listing operation and enumerate the result segment.
                var resultSegment =
                    BlobService.GetBlobContainersAsync(BlobContainerTraits.Metadata, TEST_CONTAINER_PREFIX, default)
                    .AsPages(default, segmentSize);

                await foreach (Azure.Page<BlobContainerItem> containerPage in resultSegment) {
                    foreach (BlobContainerItem containerItem in containerPage.Values) {
                        result.Add(containerItem);
                    }
                }

                return result;
            }
            catch (RequestFailedException e) {
                _sink.OnMessage(new DiagnosticMessage("Failed to get Test containers. Exception: {0}", e.Message));
                throw;
            }
        }
    }
}
