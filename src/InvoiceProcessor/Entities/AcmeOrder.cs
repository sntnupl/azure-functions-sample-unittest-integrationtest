namespace InvoiceProcessor.Entities
{
    public class AcmeOrder
    {
        public string OrderNumber { get; set; } = string.Empty;

        public string OrderDate { get; set; }

        public string CustomerName { get; set; }

        public string DeliveryAddress { get; set; }

        public double OrderTotal { get; set; }
    }
}
