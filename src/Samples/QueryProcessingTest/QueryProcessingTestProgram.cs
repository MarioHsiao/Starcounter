using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
            RunQueryProcessingTest();
#if false // Switched of while developing
            SqlBugsTest.QueryTests();
            FetchTest.RunFetchTest();
            AggregationTest.RunAggregationTest();
#endif
            CodePropertiesTesting.TestCodeProperties();
        }

        static void RunQueryProcessingTest() {
            DataPopulation.PopulateUsers(5, 3);
            DataPopulation.PopulateUsers(10000, 3);
            DataPopulation.PopulateAccounts(10000, 3);
        }
    }
}
