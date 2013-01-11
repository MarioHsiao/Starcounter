using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
            RunQueryProcessingTest();
        }

        static void RunQueryProcessingTest() {
            DataPopulation.PopulateUsers(100000, 3);
            DataPopulation.PopulateAccounts(100000, 3);
        }
    }
}
