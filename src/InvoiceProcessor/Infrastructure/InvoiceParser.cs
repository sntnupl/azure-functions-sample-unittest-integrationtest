using InvoiceProcessor.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace InvoiceProcessor.Infrastructure
{
    public class InvoiceParser : IAcmeInvoiceParser
    {
        private static readonly JsonSerializerOptions jsonDeserializeOptions = new JsonSerializerOptions {
            AllowTrailingCommas = true,
            IgnoreNullValues = false,
        };
        private static readonly string DefaultDelimiter = "==========";
        private string _delimiter;

        public InvoiceParser(string delimiter = null)
        {
            _delimiter = string.IsNullOrEmpty(delimiter) ? DefaultDelimiter : delimiter;
        }

        public bool TryParse(Stream stream, ILogger logger, out List<AcmeOrder> orders)
        {
            orders = new List<AcmeOrder>();

            try {
                using (var streamReader = new StreamReader(stream)) {
                    List<List<string>> listOfOrders = ExtractOrderLines(streamReader);

                    foreach (var orderLines in listOfOrders) {
                        var (order, err) = CreateAcmeOrder(orderLines, logger);
                        if (default == order) {
                            logger.LogError($"Error parsing invoice: {Configurations.AcmeParsingErrorMsg(err)}");
                            continue;
                        }
                        orders.Add(order);
                    }
                    return true;
                }
            }
            catch (Exception ex) {
                logger.LogError(ex, "Exception encountered during Invoice parsing.");
                return false;
            }
        }

        private (AcmeOrder, InvoiceParsingError) CreateAcmeOrder(List<string> orderLines, ILogger logger)
        {
            if (orderLines == default || orderLines.Count < 1)
                return (default, InvoiceParsingError.InvalidOrderText);

            var orderText = string.Concat(orderLines);
            if (string.IsNullOrWhiteSpace(orderText))
                return (default, InvoiceParsingError.InvalidOrderText);


            AcmeOrder parsedOrder;
            try {
                parsedOrder = JsonSerializer.Deserialize<AcmeOrder>(orderText, jsonDeserializeOptions);
            }
            catch (JsonException ex) {
                logger.LogError(ex, "Failed to parse order text");
                return (default, InvoiceParsingError.InvalidOrderJson);
            }
            catch (Exception ex) {
                logger.LogError(ex, "Unexpected Error.");
                return (default, InvoiceParsingError.UnexpectedException);
            }

            return (parsedOrder, InvoiceParsingError.None);
        }

        private List<List<string>> ExtractOrderLines(StreamReader streamReader)
        {
            string line;
            List<string> currentOrderLines = null;
            List<List<string>> result = new List<List<string>>();

            while (default != (line = streamReader.ReadLine())) {
                if (line.StartsWith(_delimiter)) {
                    if (currentOrderLines?.Count > 0)
                        result.Add(currentOrderLines);
                    currentOrderLines = new List<string>();
                    continue;
                }

                if (!string.IsNullOrEmpty(line))
                    currentOrderLines.Add(line);
            }

            return result;
        }
    }
}
