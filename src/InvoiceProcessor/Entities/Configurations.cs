using System;
using System.Collections.Generic;
using System.Text;

namespace InvoiceProcessor.Entities
{
    public enum InvoiceParsingError
    {
        None,
        InvalidOrderText,
        InvalidOrderJson,
        UnexpectedException,
    }

    public class Configurations
    {
        public static string AcmeParsingErrorMsg(InvoiceParsingError error)
        {
            return error switch {
                InvoiceParsingError.InvalidOrderText => "Invalid Orders Text.",
                InvoiceParsingError.InvalidOrderJson => "Invalid Orders Json.",
                InvoiceParsingError.UnexpectedException => "Unexpected error.",
                _ => string.Empty,
            };
        }
    }
}
