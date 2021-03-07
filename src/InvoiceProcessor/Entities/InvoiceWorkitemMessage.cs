using System;

namespace InvoiceProcessor.Entities
{
    public enum DataLocationType
    {
        Unknown,
        AzureBlob,
        AwsS3,
        LocalFile,
    }

    public class InvoiceWorkitemMessage
    {
        public string CorrelationId { get; set; }

        public Guid TransactionId { get; set; }

        public string UserEmail { get; set; }

        public DataLocationType DataLocationType { get; set; }

        public string DataLocation { get; set; }
    }
}
