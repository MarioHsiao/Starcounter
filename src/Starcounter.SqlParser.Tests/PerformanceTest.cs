using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Query.RawParserAnalyzer;

namespace Starcounter.SqlParser.Tests {
    public static class PerformanceTest {

        private static int nrIterations = 100000;

        private static void MeasureQueryPerformance(String query) {
            Stopwatch timer = new Stopwatch();
            ParserTreeWalker analyzer = new ParserTreeWalker();
            timer.Start();
            for (int i = 0; i < nrIterations; i++)
                analyzer.ParseQuery(query);
            timer.Stop();
            Console.WriteLine("Performed query: " + query);
            Console.WriteLine(nrIterations + " iterations in " + timer.ElapsedMilliseconds + " ms, " +
                (decimal)1000 * timer.ElapsedMilliseconds / nrIterations + " mcs.");
        }

        public static void PerformanceTests() {
            MeasureQueryPerformance("SELECT a FROM account a");
        }
    }
}
