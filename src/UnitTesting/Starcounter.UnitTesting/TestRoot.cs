
using System;
using System.Collections.Generic;

namespace Starcounter.UnitTesting
{
    public class TestRoot : ITestRoot
    {
        public readonly string Database;

        readonly Dictionary<string, TestHost> appHosts = new Dictionary<string, TestHost>();

        public TestRoot(string database)
        {
            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentNullException(nameof(database));
            }

            Database = database;
        }

        public IEnumerable<TestHost> Hosts {
            get {
                return appHosts.Values;
            }
        }

        public TestHost IncludeNewHost(string application)
        {
            if (string.IsNullOrEmpty(application))
            {
                throw new ArgumentNullException(nameof(application));
            }

            TestHost host;
            var exist = appHosts.TryGetValue(application, out host);
            if (exist)
            {
                return host;
            }

            host = TestHostFactory.CreateHost();
            appHosts.Add(application, host);

            return host;
        }
    }
}
