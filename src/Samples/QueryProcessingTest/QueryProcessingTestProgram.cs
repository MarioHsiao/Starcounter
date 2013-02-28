using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
            RunQueryProcessingTest();
            SqlBugsTest.QueryTests();
            FetchTest.RunFetchTest();
            AggregationTest.RunAggregationTest();
            CodePropertiesTesting.TestCodeProperties();
#if false // Not supported
            SelectClauseExpressionsTests.TestSelectClauseExpressions();
#endif
        }

        static void RunQueryProcessingTest() {
            TestErrorMessages.RunTestErrorMessages();
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
        }
    }
}
