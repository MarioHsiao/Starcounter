using System;

namespace FasterThanJson.Tests {
    class FasterThanJsonProgram {
        static void Main(string[] args) {
            InvestigateTuplePerformance.RunAllTests();
            TestIndexRead.RandomIndexAccessTest();
        }
    }
}
