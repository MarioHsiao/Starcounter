using System;

namespace Starcounter.SqlProcessor.Tests {
    class SqlProcessorTestProgram {
        static void Main() {
            //SqlProcessorTests.HelloProcessor();
            //SqlProcessorTests.SqlSyntax();
            MultiThreadedMemoryLeakTests.SequentialTest();
            MultiThreadedMemoryLeakTests.MultithreadedTest();
        }
    }
}
