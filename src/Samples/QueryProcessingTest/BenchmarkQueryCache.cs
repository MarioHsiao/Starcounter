using System;
using System.Diagnostics;
using System.Threading;
using Starcounter;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    public static class BenchmarkQueryCache {
        volatile static Int32 nrFinishedWorkers = 0;
        readonly static String query = "select a from account a where accountid = ?";
        
        public static void BenchQueryCache() {
            HelpMethods.LogEvent("Start benchmark of query cache");
            int nrIterations = 1000000;
            int sleepTimeout = 100;
            if (TestLogger.IsRunningOnBuildServer()) {
                nrIterations = nrIterations * 100;
                sleepTimeout = sleepTimeout * 10;
            }
            for (int schedulers = 1; schedulers <= Environment.ProcessorCount; schedulers++) {
                Stopwatch timer = new Stopwatch();
                nrFinishedWorkers = 0;
                timer.Start();
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                    DbSession dbs = new DbSession();
                    dbs.RunAsync(() => QueryEnumerator(2), schedulerId);
                }
                while (nrFinishedWorkers != schedulers)
                    Thread.Sleep(sleepTimeout);
                timer.Stop();
                HelpMethods.LogEvent("Warm up of " + schedulers + " schedulers took " + timer.ElapsedMilliseconds + " ms.");
                BenchmarkAction(schedulers, nrIterations, () => QueryEnumerator(nrIterations), "Obtaining enumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => DbSQL(nrIterations), "Calling Db.SQL on ");
                BenchmarkAction(schedulers, nrIterations, () => GetEnumerator(nrIterations), "Calling GetEnumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => GetSchedulerInstance(nrIterations), "Getting a scheduler instance on ");
                BenchmarkAction(schedulers, nrIterations, () => GetCachedEnumerator(nrIterations), "Getting cached enumerator on ");
                HelpMethods.LogEvent("Finished benchmark of query cache");
            }
        }

        public static void BenchmarkAction(int nrSchedulers, int nrIterations, Action work, String prefix) {
            Stopwatch timer = new Stopwatch();
            nrFinishedWorkers = 0;
            timer.Start();
            for (byte schedulerId = 0; schedulerId < nrSchedulers; schedulerId++) {
                DbSession dbs = new DbSession();
                dbs.RunAsync(work, schedulerId);
            }
            while (nrFinishedWorkers != nrSchedulers)
                Thread.Sleep(100);
            timer.Stop();
            HelpMethods.LogEvent(prefix + nrSchedulers + " schedulers took " + timer.ElapsedMilliseconds +
                " ms for " + nrIterations + " iterations on each scheduler, i.e., " +
                (long)nrIterations * nrSchedulers * 1000 / timer.ElapsedMilliseconds +
                " accesses per second.");
        }

        public static void QueryEnumerator(int nrIterations) {
            for (int i = 0; i < nrIterations; i++) {
                var results = Db.SQL(query, 10).GetEnumerator();
                results.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void DbSQL(int nrIterations) {
            for (int i=0; i<nrIterations;i++)
                Db.SQL(query, 10);
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetEnumerator(int nrIterations) {
            var sqlResults = Db.SQL(query, 10);
            for (int i = 0; i < nrIterations; i++) {
                var results = sqlResults.GetEnumerator();
                results.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetSchedulerInstance(int nrIterations) {
            for (int i = 0; i < nrIterations; i++)
                Scheduler.GetInstance();
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetCachedEnumerator(int nrIterations) {
            var scheduler = Scheduler.GetInstance();
            Type t = typeof(Object);
            for (int i = 0; i < nrIterations; i++) {
                var e = scheduler.SqlEnumCache.GetCachedEnumerator(query, t);
                e.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }
    }
}
