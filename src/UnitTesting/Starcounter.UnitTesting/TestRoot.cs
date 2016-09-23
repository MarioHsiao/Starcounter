
using System;
using System.Collections.Generic;

namespace Starcounter.UnitTesting
{
    public class TestRoot : ITestRoot
    {
        public readonly string Database;

        public readonly ITestHostFactory DefaultTestHostFactory;

        readonly Dictionary<string, TestHost> appHosts = new Dictionary<string, TestHost>();

        public TestRoot(string database, ITestHostFactory defaultFactory)
        {
            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (defaultFactory == null)
            {
                throw new ArgumentNullException(nameof(defaultFactory));
            }

            Database = database;
            DefaultTestHostFactory = defaultFactory;
        }

        public IEnumerable<TestHost> Hosts {
            get {
                return appHosts.Values;
            }
        }

        public TestHost IncludeNewHost(string application, ITestHostFactory factory = null)
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

            var hostFactory = factory ?? DefaultTestHostFactory;

            host = hostFactory.CreateHost();
            host.Name = application;

            appHosts.Add(application, host);

            return host;
        }
    }
}
