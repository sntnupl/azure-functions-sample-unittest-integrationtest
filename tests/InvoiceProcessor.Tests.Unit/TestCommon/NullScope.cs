using System;

namespace InvoiceProcessor.Tests.Unit.TestCommon
{
    public class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();

        private NullScope() { }

        public void Dispose()
        { }
    }
}
