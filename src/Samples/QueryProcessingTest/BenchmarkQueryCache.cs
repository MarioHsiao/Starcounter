using System;
using System.Diagnostics;
using System.Threading;
using Starcounter;
using Starcounter.Query.Execution;
using Starcounter.TestFramework;

namespace QueryProcessingTest {
    public static class BenchmarkQueryCache {
        volatile static Int32 nrFinishedWorkers = 0;
        readonly static String query = "select a from account a where accountid = ?";
        
        public static void BenchQueryCache() {
            HelpMethods.LogEvent("Start benchmark of query cache");
            GC.WaitForFullGCComplete(1000);
            int nrIterations = 1000000;
#if true
            int nrIterationsNoDispose = 10000;
#endif
            //int sleepTimeout = 100;
            if (TestLogger.IsRunningOnBuildServer()) {
                nrIterations = nrIterations * 10;
                //sleepTimeout = sleepTimeout * 10;
            }
            for (int schedulers = 1; schedulers <= Environment.ProcessorCount; schedulers++) {
                Stopwatch timer = new Stopwatch();
                nrFinishedWorkers = 0;
                timer.Start();
                for (byte schedulerId = 0; schedulerId < schedulers; schedulerId++) {
                    Scheduling.ScheduleTask(() => QueryEnumerator(2), false, schedulerId);
                }
                while (nrFinishedWorkers != schedulers)
                    Thread.Sleep(100);
                timer.Stop();
                GC.WaitForFullGCComplete(1000);
                HelpMethods.LogEvent("Warm up of " + schedulers + " schedulers took " + timer.ElapsedMilliseconds + " ms.");
                BenchmarkAction(schedulers, nrIterations, () => QueryEnumerator(nrIterations), "Obtaining enumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => DbSQL(nrIterations), "Calling Db.SQL on ");
                BenchmarkAction(schedulers, nrIterations, () => GetEnumerator(nrIterations), "Calling GetEnumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => GetExecutionEnumerator(nrIterations), "Calling GetExecutionEnumerator on ");
                //BenchmarkAction(schedulers, nrIterations, () => NewSqlEnumerator(nrIterations), "Creating new SqlEnumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => GetSchedulerInstance(nrIterations), "Getting a scheduler instance on ");
                BenchmarkAction(schedulers, nrIterations, () => GetType<Object>(nrIterations), "Calling typeof on ");
                BenchmarkAction(schedulers, nrIterations, () => GetCachedEnumerator(nrIterations), "Getting cached enumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => RestExecutionEnumerator(nrIterations, 10), "Rest of GetExecutionEnumerator on ");
                BenchmarkAction(schedulers, nrIterations, () => GetEnumeratorAndMove(nrIterations), "Obtaining enumerator and move next on ");
#if true
                BenchmarkAction(schedulers, nrIterationsNoDispose, () => GetEnumerator(nrIterationsNoDispose), "Calling GetEnumerator on ");
                BenchmarkAction(schedulers, nrIterationsNoDispose, () => GetEnumeratorNoDispose(nrIterationsNoDispose), "Calling GetEnumerator with no dispose on ");
                BenchmarkAction(schedulers, nrIterationsNoDispose, () => QueryEnumerator(nrIterationsNoDispose), "Obtaining enumerator on ");
                BenchmarkAction(schedulers, nrIterationsNoDispose, () => QueryEnumeratorNoDispose(nrIterationsNoDispose), "Obtaining enumerator with no dispose on ");
                BenchmarkAction(schedulers, nrIterationsNoDispose / schedulers, () => GetEnumeratorAndMove(nrIterationsNoDispose / schedulers), "Obtaining enumerator and move next on ");
                BenchmarkAction(schedulers, nrIterationsNoDispose / schedulers, () => GetEnumeratorAndMoveNoDispose(nrIterationsNoDispose / schedulers), 
                    "Obtaining enumerator and move next with no dispose on ");
#endif
                GC.WaitForFullGCComplete(1000);
            }
            HelpMethods.LogEvent("Finished benchmark of query cache");
        }

        public static void BenchmarkAction(int nrSchedulers, int nrIterations, Action work, String prefix) {
            Thread.Sleep(1000);
            Stopwatch timer = new Stopwatch();
            nrFinishedWorkers = 0;
            timer.Start();
            for (byte schedulerId = 0; schedulerId < nrSchedulers; schedulerId++) {
                Scheduling.ScheduleTask(work, false, schedulerId);
            }
            while (nrFinishedWorkers != nrSchedulers)
                Thread.Sleep(100);
            timer.Stop();
            HelpMethods.LogEvent(prefix + nrSchedulers + " schedulers took " + timer.ElapsedMilliseconds +
                " ms for " + nrIterations + " iterations on each scheduler, i.e., " +
                (long)nrIterations * nrSchedulers * 1000 / timer.ElapsedMilliseconds +
                " accesses per second.");
            GC.WaitForFullGCComplete(1000);

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
            QueryResultRows<dynamic> s;
            for (int i=0; i<nrIterations;i++)
                s = Db.SQL(query, 10);
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

        public static void GetEnumeratorNoDispose(int nrIterations) {
            var sqlResults = Db.SQL(query, 10);
            for (int i = 0; i < nrIterations; i++) {
                var results = sqlResults.GetEnumerator();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetExecutionEnumerator(int nrIterations) {
            var sqlResults = Db.SQL(query, 10);
            for (int i = 0; i < nrIterations; i++) {
                var results = sqlResults.GetExecutionEnumerator();
                results.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void NewSqlEnumerator(int nrIterations) {
            var sqlResults = Db.SQL(query, 10);
            var results = sqlResults.GetExecutionEnumerator();
            for (int i = 0; i < nrIterations; i++) {
                var s = new SqlEnumerator<dynamic>(results);
            }
            results.Dispose();
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetSchedulerInstance(int nrIterations) {
            Scheduler s;
            for (int i = 0; i < nrIterations; i++)
                s = Scheduler.GetInstance();
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetType<T>(int nrIterations) {
            Type t = null;
            for (int i = 0; i < nrIterations; i++)
                t = typeof(T);
            lock (query) {
                nrFinishedWorkers++;
            }
            if (t != null)
                t.ToString();
        }

        public static void GetCachedEnumerator(int nrIterations) {
            var scheduler = Scheduler.GetInstance();
            for (int i = 0; i < nrIterations; i++) {
                var e = scheduler.SqlEnumCache.GetCachedEnumerator<dynamic>(query, false);
                e.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void RestExecutionEnumerator(int nrIterations, params Object[] sqlParams) {
            Boolean slowSQL = false;
            var scheduler = Scheduler.GetInstance();
            var execEnum = scheduler.SqlEnumCache.GetCachedEnumerator<dynamic>(query,false);
            for (int i = 0; i < nrIterations; i++) {
                if (execEnum.QueryFlags != QueryFlags.None && !slowSQL) {
                    if ((execEnum.QueryFlags & QueryFlags.IncludesAggregation) != QueryFlags.None)
                        throw ErrorCode.ToException(Error.SCERRUNSUPPORTAGGREGATE, "Method Starcounter.Db.SQL does not support queries with aggregates.");
                    //throw new SqlException("Method Starcounter.Db.SQL does not support queries with aggregates.");

                    if ((execEnum.QueryFlags & QueryFlags.IncludesLiteral) != QueryFlags.None)
                        if (String.IsNullOrEmpty(execEnum.LiteralValue))
                            throw ErrorCode.ToException(Error.SCERRUNSUPPORTLITERAL, "Method Starcounter.Db.SQL does not support queries with literals. Use variable and parameter instead.");
                        else
                            throw ErrorCode.ToException(Error.SCERRUNSUPPORTLITERAL, "Method Starcounter.Db.SQL does not support queries with literals. Found literal is " +
                                execEnum.LiteralValue + ". Use variable and parameter instead.");
                    //throw new SqlException("Method Starcounter.Db.SQL does not support queries with literals. Use variable and parameter instead.");
                }

                // Setting SQL parameters if any are given.
                if (sqlParams != null)
                    execEnum.SetVariables(sqlParams);

            }
            execEnum.Dispose();
            lock (query) {
                nrFinishedWorkers++;
            }
        }
    
        public static void QueryEnumeratorNoDispose(int nrIterations) {
            for (int i = 0; i < nrIterations; i++) {
                var results = Db.SQL(query, 10).GetEnumerator();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetEnumeratorAndMove(int nrIterations) {
            for (int i = 0; i < nrIterations; i++) {
                var results = Db.SQL(query, 10).GetEnumerator();
                Debug.Assert(results.MoveNext());
                results.Dispose();
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }

        public static void GetEnumeratorAndMoveNoDispose(int nrIterations) {
            for (int i = 0; i < nrIterations; i++) {
                var results = Db.SQL(query, 10).GetEnumerator();
                Debug.Assert(results.MoveNext());
            }
            lock (query) {
                nrFinishedWorkers++;
            }
        }
    }
}
