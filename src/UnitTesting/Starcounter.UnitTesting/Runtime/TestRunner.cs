
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.UnitTesting.Runtime
{
    public class TestRunner
    {
        public static IEnumerable<TestResult> Run(TestRoot root)
        {
            // Filter on hosts? Filter provided by caller?
            // TODO:

            return Run(root.Database, root.Hosts.ToArray());
        }

        public static TestResult Run(string database, TestHost host)
        {
            return Run(database, new[] { host }).First();
        }

        public static IEnumerable<TestResult> Run(string database, IEnumerable<TestHost> hosts)
        {
            var results = new List<TestResult>();

            var header = ResultHeader.NewStartingNow(database);

            var writer = new ResultWriter(header);

            foreach (var host in hosts)
            {
                var result = new TestResult();
                result.Database = database;
                result.Application = host.Name;
                result.Started = DateTime.Now;
                
                var hostWriter = writer.OpenHostWriter(host);
                
                host.Run(result, hostWriter.Writer);

                result.Finished = DateTime.Now;
                results.Add(result);

                writer.CloseHostWriterWithResult(hostWriter,result);
            }

            var footer = new ResultFooter();
            footer.Ended = DateTime.Now;

            writer.Close(footer);

            return results;
        }
    }
}