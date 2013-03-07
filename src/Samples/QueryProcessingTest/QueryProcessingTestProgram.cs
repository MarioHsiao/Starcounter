using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
            QueryProcessingPerformance.MeasurePrepareQuery();
            QueryProcessingPerformance.MeasurePrepareQuery();
            RunQueryProcessingTest();
            SqlBugsTest.QueryTests();
            FetchTest.RunFetchTest();
            AggregationTest.RunAggregationTest();
            CodePropertiesTesting.TestCodeProperties();
            SelectClauseExpressionsTests.TestSelectClauseExpressions();
            Environment.Exit(0);
        }

        static void RunQueryProcessingTest() {
            TestErrorMessages.RunTestErrorMessages();
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
        }
    }
}
