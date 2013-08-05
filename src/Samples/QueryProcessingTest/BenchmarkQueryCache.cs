using System;
using System.Diagnostics;
using System.Threading;
using Starcounter;

namespace QueryProcessingTest {
    public static class BenchmarkQueryCache {
        volatile static Int32 nrFinishedWorkers = 0;
        readonly static String query = "select a from account a where accountid = ?";
        
        public static void BenchQueryCache() {
            HelpMethods.LogEvent("Start benchmark of query cache");
            int nrIterations = 1000000;
            for (int schedulers = 1; schedulers <= Environment.ProcessorCount; schedulers++) {
                Stopwatch timer = new Stopwatch();
                nrFinishedWorkers = 0;
                timer.Start();
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                    DbSession dbs = new DbSession();
                    dbs.RunAsync(() => GetEnumerator(2), schedulerId);
                }
                while (nrFinishedWorkers != schedulers)
                    Thread.Sleep(100);
                timer.Stop();
                HelpMethods.LogEvent("Worm up of " + schedulers + " schedulers took " + timer.ElapsedMilliseconds + " ms.");
                nrFinishedWorkers = 0;
                timer.Reset();
                timer.Start();
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                    DbSession dbs = new DbSession();
                    dbs.RunAsync(() => GetEnumerator(nrIterations), schedulerId);
                }
                while (nrFinishedWorkers != schedulers)
                    Thread.Sleep(1000);
                timer.Stop();
                HelpMethods.LogEvent("Accessing cache on " + schedulers + " schedulers took " + timer.ElapsedMilliseconds +
                    " ms for " + nrIterations + " iterations on each scheduler, i.e., " + 
                    nrIterations*schedulers*1000 /timer.ElapsedMilliseconds+
                    " cache accesses per second.");
                HelpMethods.LogEvent("Finished benchmark of query cache");
            }
        }

        public static void GetEnumerator(int nrIterations) {
            for (int i = 0; i < nrIterations; i++) {
                var results = Db.SQL(query, 10).GetEnumerator();
                results.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }
    }
}
