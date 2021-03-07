using Microsoft.Azure.Cosmos.Table;
using System.Text.Json;

namespace InvoiceProcessor.Entities
{
    public class AcmeOrderEntry : TableEntity
    {
        public AcmeOrderEntry()
        { }

        public AcmeOrderEntry(string userId, AcmeOrder order)
        {
            this.PartitionKey = userId;
            this.RowKey = order.OrderNumber;
            this.Order = order;
        }


        [IgnoreProperty]
        public AcmeOrder Order {
            get {
                return JsonSerializer.Deserialize<AcmeOrder>(AcmeOrderRaw);
            }

            set {
                AcmeOrderRaw = JsonSerializer.Serialize(value);
            }
        }

        public string AcmeOrderRaw { get; set; }
    }
}
