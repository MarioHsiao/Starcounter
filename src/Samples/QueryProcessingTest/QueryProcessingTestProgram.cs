using System;
using System.Diagnostics;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static TraceListener defaultTraceListener = null;

        static void Main(string[] args) {
            try {
                HelpMethods.LogEvent("Query processing tests are started");
                UpdateTraceListener();
                BindingTestDirect.DirectBindingTest();
                HelpMethods.LogEvent("Test query preparation performance.");
                QueryProcessingPerformance.MeasurePrepareQuery();
                TestErrorMessages.RunTestErrorMessages();
                PopulateData();
                SqlBugsTest.QueryTests();
                FetchTest.RunFetchTest();
                AggregationTest.RunAggregationTest();
                CodePropertiesTesting.TestCodeProperties();
                SelectClauseExpressionsTests.TestSelectClauseExpressions();
                OffsetkeyTest.Master();
                ObjectIdentityTest.TestObjectIdentityInSQL();
                HelpMethods.LogEvent("All tests completed");
            } catch (Exception e) {
                HelpMethods.LogEvent(e.ToString());
                throw e;
            }
            Environment.Exit(0);
        }

        static void PopulateData() {
            HelpMethods.LogEvent("Data population");
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
            HelpMethods.LogEvent("Finished data population");
        }

        static void UpdateTraceListener() {
            if (Environment.GetEnvironmentVariable("SC_RUNNING_ON_BUILD_SERVER") == "True") {
                foreach (TraceListener l in Trace.Listeners) {
                    if (l is DefaultTraceListener)
                        defaultTraceListener = l;
                }
                Trace.Listeners.Remove(defaultTraceListener);
                Trace.Listeners.Add(new Starcounter.Internal.ErrorHandling.TestTraceListener("QueryProcessingListener"));
            }
            Trace.Assert(false);
        }
    }
}
