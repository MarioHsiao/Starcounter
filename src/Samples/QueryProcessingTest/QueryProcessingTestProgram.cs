using System;

namespace QueryProcessingTest {
    class QueryProcessingTestProgram {
        static void Main(string[] args) {
            BindingTestDirect.DirectBindingTest();
        }

        static void RunQueryProcessingTest() {
            DataPopulation.PopulateAccounts(100000, 3);
        }
    }
}
