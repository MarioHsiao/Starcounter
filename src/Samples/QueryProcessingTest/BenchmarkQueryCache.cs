using System;
using System.Diagnostics;
using Starcounter;

namespace QueryProcessingTest {
    public static class BenchmarkQueryCache {
        public static void BenchQueryCache() {
            HelpMethods.LogEvent("Start benchmark of query cache");
            String query = "select a from account a where accountid = ?";
            int nrIterations = 100000;
            for (int schedulers = 1; schedulers <= Environment.ProcessorCount; schedulers++) {
                Stopwatch timer = new Stopwatch();
                DbSession dbs = new DbSession();
                timer.Start();
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                        dbs.RunAsync(() => Db.SQL(query, 10).GetEnumerator(), schedulerId);
                }
                timer.Stop();
                HelpMethods.LogEvent("Worm up of " + schedulers + " schedulers took " + timer.ElapsedMilliseconds + " ms.");
                timer.Reset();
                timer.Start();
                for (int i =0; i<nrIterations;i++)
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                    dbs.RunAsync(() => { Db.SQL(query, 10).GetEnumerator() }, schedulerId);
                }
                timer.Stop();
                HelpMethods.LogEvent("Accessing cache on " + schedulers + " schedulers took " + timer.ElapsedMilliseconds +
                    " ms for " + nrIterations + " iterations, i.e., " + (double)1000 * timer.ElapsedMilliseconds / nrIterations +
                    " mcs.");
                HelpMethods.LogEvent("Finished benchmark of query cache");
            }
        }
    }
}
