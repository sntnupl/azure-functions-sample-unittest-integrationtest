using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;

namespace InvoiceProcessor.Tests.Integration.TestCommon
{
    public class FakeTypeLocator : ITypeLocator
    {
        private readonly Type[] _types;

        public FakeTypeLocator(params Type[] types)
        {
            _types = types;
        }

        public IReadOnlyList<Type> GetTypes()
        {
            return _types;
        }
    }
}
