using Microsoft.Azure.WebJobs;
using System;

namespace InvoiceProcessor.Tests.Integration.TestCommon
{
    public class RandomNameResolver : INameResolver
    {
        // Convert to lowercase because many Azure services expect only lowercase
        private readonly string _randomString = Guid.NewGuid().ToString("N").ToLower();

        public virtual string Resolve(string name)
        {
            if (name == "rnd") {
                return _randomString;
            }

            return null;
        }

        public string ResolveInString(string input)
        {
            return input.Replace("%rnd%", _randomString);
        }
    }
}
