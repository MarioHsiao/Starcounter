using System;

namespace Starcounter.SqlProcessor.Tests {
    class SqlProcessorTestProgram {
        static void Main() {
            SqlProcessorTests.HelloProcessor();
            SqlProcessorTests.SqlSyntax();
            MultiThreadedMemoryLeakTests.IsFromCMD = true;
            MultiThreadedMemoryLeakTests.SequentialTest();
            MultiThreadedMemoryLeakTests.MultithreadedTest();
        }
    }
}
