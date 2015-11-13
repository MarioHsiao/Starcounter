using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Starcounter.SqlProcessor.Tests {
    [TestFixture]
    public static class PerformanceTest {
        private static int nrIterations = 100000;

        private static void MeasureQueryPerformance(String query) {
            Stopwatch timer = new Stopwatch();
            byte queryType;
            ulong iterator;
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                SqlProcessor.CallSqlProcessor(query, out queryType, out iterator);
            timer.Stop();
            Console.WriteLine("Performed query: " + query);
            Console.WriteLine(nrIterations + " iterations in " + timer.ElapsedMilliseconds + " ms, " +
                (decimal)1000 * timer.ElapsedMilliseconds / nrIterations + " mcs.");
        }

        [Test]
        [Category("LongRunning")]
        public static void PerformanceTests() {
            MeasureQueryPerformance("SELECT a FROM account a");
        }
    }
}
