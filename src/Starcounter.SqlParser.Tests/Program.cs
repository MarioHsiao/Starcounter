using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.SqlParser.Tests {
    static class Program {
        static void Main(string[] args) {
            Console.WriteLine("Unit tests of SQL Parser.");
            //PerformanceTest.PerformanceTests();
            TestSqlParser.TestScannerWcharFixForErrors();
            TestSqlParser.ParseQueriesForErrors();
            TestSqlParser.MultithreadedTest();
            Console.WriteLine("Test completed.");
        }
    }
}
