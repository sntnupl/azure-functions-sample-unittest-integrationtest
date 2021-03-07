using InvoiceProcessor.Entities;
using InvoiceProcessor.Infrastructure;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceProcessor
{
    public class InvoiceWorkitemEventHandler
    {
        private static readonly JsonSerializerOptions jsonSerializationOptions = new JsonSerializerOptions {
            AllowTrailingCommas = true,
            IgnoreNullValues = false,
            PropertyNameCaseInsensitive = true,
        };


        public static IAcmeInvoiceParser InvoiceParser = new InvoiceParser();


        [FunctionName("InvoiceWorkitemEventHandler")]
        public static async Task Run(
            [ServiceBusTrigger("workiteminvoice", "InvoiceProcessor", Connection = "ServiceBusReceiverConnection")] string msg,
            IBinder blobBinder,
            [Table("AcmeOrders", Connection = "StorageConnection")] IAsyncCollector<AcmeOrderEntry> tableOutput,
            ILogger logger)
        {
            InvoiceWorkitemMessage invoiceWorkItem;
            logger.LogInformation($"C# ServiceBus topic trigger function processed message: {msg}");



            if (string.IsNullOrEmpty(msg)) {
                logger.LogError("Empty Invoice Workitem.");
                return;
            }

            try {
                invoiceWorkItem = JsonSerializer.Deserialize<InvoiceWorkitemMessage>(msg, jsonSerializationOptions);
            }
            catch (JsonException ex) {
                logger.LogError(ex, "Invalid Invoice Workitem.");
                return;
            }
            catch (Exception ex) {
                logger.LogError(ex, "Unexpected Error.");
                return;
            }


            if (invoiceWorkItem.DataLocationType != DataLocationType.AzureBlob) {
                logger.LogError($"Unsupported data location type {invoiceWorkItem.DataLocationType}.");
                return;
            }

            if (string.IsNullOrEmpty(invoiceWorkItem.DataLocation)) {
                logger.LogError("Empty data location.");
                return;
            }


            var blobAttr = new BlobAttribute(invoiceWorkItem.DataLocation, FileAccess.Read) {
                Connection = "StorageConnection"
            };

            using (var blobStream = await blobBinder.BindAsync<Stream>(blobAttr)) {
                if (!InvoiceParser.TryParse(blobStream, logger, out List<AcmeOrder> orders)) {
                    logger.LogError("Failed to parse invoice.");
                    return;
                }

                foreach (var order in orders) {
                    await tableOutput.AddAsync(new AcmeOrderEntry(invoiceWorkItem.UserEmail, order));
                    logger.LogInformation($"Added table record for BigBasket Order number: {order.OrderNumber}");
                }
            }
        }
    }
}
