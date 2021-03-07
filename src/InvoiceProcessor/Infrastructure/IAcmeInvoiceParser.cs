using InvoiceProcessor.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;

namespace InvoiceProcessor.Infrastructure
{
    public interface IAcmeInvoiceParser
    {
        bool TryParse(Stream reader, ILogger logger, out List<AcmeOrder> orders);
    }
}
