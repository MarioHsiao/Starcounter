using Microsoft.CSharp;
using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.IO;
using Starcounter.TestFramework;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace LoadAndLatency
{
    // Core LoadAndLatency engine class.
    public class LoadAndLatencyCore
    {
        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceCounter(out UInt64 lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        static extern bool QueryPerformanceFrequency(out UInt64 lpFrequency);

        // Special identifier for single worker scheduler.
        const Int32 SingleWorkerSchedId = 255;

        // Minimum number of workers during nightly build.
        public Int32 MinNightlyWorkers = 5;

        // Performance timer frequency (counts per microsecond).
        Double timerFreqTicksPerMcs = 0;

        // Indicates if library is started on the client.
        Boolean startedOnClient = false;

        // Object creation transactions multiplier.
        public Int32 TransactionsMagnifier = 100;

        // Number of objects per each transaction.
        Int32 NumObjectsPerTransaction = 1000;

        // Number of retries on update transaction.
        const Int32 TransRetriesNum = 50;

        // Addition on workers amount.
        Int32 additionalWorkersNum = 0;

        // Total number of objects in database.
        Int32 TotalNumOfObjectsInDB = 0;

        // Number of repeats and query types.
        const Int32 NumOfRepeats = 5;

        // All SELECT queries.
        readonly String[] QueryStrings = new String[]
        {
            "SELECT c FROM TestClass c WHERE c.prop_int64 = ?",
            "SELECT c FROM TestClass c WHERE c.prop_string = ?",
            "SELECT c FROM TestClass c WHERE c.prop_datetime = ?",
            "SELECT c FROM TestClass c WHERE c.prop_string like ?",
            "SELECT c FROM TestClass c WHERE c.prop_string starts with ?",
            "SELECT c FROM TestClass c WHERE c.prop_decimal = ?"
        };

        // Indicates SELECT data type.
        readonly String[] QueryStatDataString = new String[]
        {
            "Int64",
            "String",
            "Datetime",
            "StringLike",
            "StringStartsWith",
            "Decimal"
        };

        // Defines simple object operations to test.
        enum SimpleObjectOperations
        {
            SIMPLE_ATTR_INT_SHADOW_UPDATE_WITH_QUERY,
            SIMPLE_OBJECTS_PURGE,

            SIMPLE_INITIAL_SHUFFLE,
            SIMPLE_OBJECT_INSERT,
            SIMPLE_CACHE_QUERIES,

            SIMPLE_ATTR_INT_READ_WITH_QUERY,
            SIMPLE_ATTR_STR_READ_WITH_QUERY,

            SIMPLE_ATTR_INT_UPDATE_WITH_QUERY,
            SIMPLE_ATTR_STR_UPDATE_WITH_QUERY,

            SIMPLE_SAVE_OBJECT_WITH_QUERY,

            SIMPLE_ATTR_INT_READ_FROM_SAVED_OBJECT,
            SIMPLE_ATTR_STR_READ_FROM_SAVED_OBJECT,

            SIMPLE_ATTR_INT_UPDATE_IN_SAVED_OBJECT,
            SIMPLE_ATTR_STR_UPDATE_IN_SAVED_OBJECT,

            SIMPLE_ATTR_INT_UPDATE_WITH_QUERY_PLUS_SNAPSHOT_ISOLATION,
            SIMPLE_ATTR_STR_UPDATE_WITH_QUERY_PLUS_SNAPSHOT_ISOLATION,

            SIMPLE_ATTR_INT_UPDATE_IN_SAVED_OBJECT_PLUS_SNAPSHOT_ISOLATION,
            SIMPLE_ATTR_STR_UPDATE_IN_SAVED_OBJECT_PLUS_SNAPSHOT_ISOLATION,

            SIMPLE_ATTR_INT_UPDATE_WITH_RANGE_QUERY,
            SIMPLE_ATTR_STR_INT_UPDATE_SAME_WITH_RANGE_QUERY,
            SIMPLE_OBJECT_DELETE_WITH_QUERY
        }

        public enum LALSpecificTestType
        {
            LAL_DEFAULT_TEST,
            LAL_PARALLEL_READ_ONLY_TEST,
            LAL_PARALLEL_UPDATES_TEST
        }

        // Specific LAL test type if any.
        public LALSpecificTestType SpecificTestType = LALSpecificTestType.LAL_DEFAULT_TEST;

        // Number of different query types.
        readonly Int32 NumOfQueryTypes = 0;

        // Represents the number of workers to run in parallel.
        public Int32 NumOfWorkers = 0;

        // Represents the number of logical cores in machine.
        public Int32 NumOfLogProc = 0;

        // Total time of different query types.
        Double g_totalTimeMcsUnsafe = 0;

        // Time of the last finished test.
        UInt64 g_endTimeTicksUnsafe;

        // Indicates that all threads has finished their work.
        volatile Int32 g_numWorkersFinishedUnsafe = 0;

        // Some of nanoseconds per object per operation.
        Double g_nsPerObjectUnsafe = 0;

        // Simple query with condition.
        const string SimpleSelectIntQuery = "SELECT s FROM SimpleObject s WHERE s.fetchInt = ?";

        // Worker parameters holder.
        class WorkerParamClass
        {
            Byte schedulerId;
            Int32 queryId;
            Boolean updatesOn;

            public Byte SchedulerId { get { return schedulerId; } }
            public Int32 QueryId { get { return queryId; } }
            public Boolean UpdatesOn { get { return updatesOn; } }

            public WorkerParamClass(Byte schedulerId, Int32 queryId, Boolean updatesOn)
            {
                this.schedulerId = schedulerId;
                this.queryId = queryId;
                this.updatesOn = updatesOn;
            }
        }

        // Worker parameters holder.
        class SimpleTestParamClass
        {
            Int32 workerGlobalOffset;
            SimpleObjectOperations testType;
            Int32 numTransactions;
            Int32 numObjectsPerTransaction;

            public Int32 WorkerGlobalOffset { get { return workerGlobalOffset; } }
            public SimpleObjectOperations TestType { get { return testType; } }
            public Int32 NumTransactions { get { return numTransactions; } }
            public Int32 NumObjectsPerTransaction { get { return numObjectsPerTransaction; } }

            public SimpleTestParamClass(
                Int32 workerGlobalOffset,
                SimpleObjectOperations testType,
                Int32 numTransactions,
                Int32 numObjectsPerTransaction)
            {
                this.workerGlobalOffset = workerGlobalOffset;
                this.testType = testType;
                this.numTransactions = numTransactions;
                this.numObjectsPerTransaction = numObjectsPerTransaction;
            }
        }

        // Resets all unsafe timers/counters.
        void ResetUnsafeTimers()
        {
            // Cleaning timers.
            g_totalTimeMcsUnsafe = 0;
            g_endTimeTicksUnsafe = 0;
            g_nsPerObjectUnsafe = 0;

            // Reseting worker finishing indicator.
            g_numWorkersFinishedUnsafe = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public LoadAndLatencyCore(Boolean startedOnClient)
        {
            // Determine performance timer frequency.
            UInt64 freqTempVar = 0;
            if (QueryPerformanceFrequency(out freqTempVar) == false)
            {
                // High-performance counter not supported
                throw new Exception("High-performance counter not supported.");
            }

            // Ticks per microsecond.
            timerFreqTicksPerMcs = freqTempVar / 1000000.0;

            // Determining number of objects in database.
            TotalNumOfObjectsInDB = TransactionsMagnifier * NumObjectsPerTransaction;
            NumOfQueryTypes = QueryStrings.Length;

            this.startedOnClient = startedOnClient;
        }

        // Determines if its a nightly build.
        public void CheckBuildType()
        {
            // Checking if its a scheduled nightly build.
            if (TestLogger.IsNightlyBuild())
            {
                additionalWorkersNum = 0;
                TotalNumOfObjectsInDB = TransactionsMagnifier * NumObjectsPerTransaction;
            }
        }

        /// <summary>
        /// Changes transactions magnifier to a new value.
        /// </summary>
        public void ChangeTransactionsMagnifier(Int32 transactionsMagnifier)
        {
            TransactionsMagnifier = transactionsMagnifier;
            TotalNumOfObjectsInDB = TransactionsMagnifier * NumObjectsPerTransaction;
        }

        /// <summary>
        /// Logs some important event both for client and server side execution.
        /// </summary>
        public void LogEvent(String eventString)
        {
            Console.WriteLine(eventString);
        }

        /// <summary>
        /// Main entry point for all LoadAndLatency tests.
        /// </summary>
        public UInt32 EntryPoint()
        {
            //System.Diagnostics.Debugger.Break();

            // Checking if we need to skip the process.
            if ((!startedOnClient) && (TestLogger.SkipInProcessTests()))
            {
                // Creating file indicating finish of the work.
                Console.WriteLine("LoadAndLatency in-process test is skipped!");

                return 0;
            }

            LogEvent("--- Starting LoadAndLatency Test...");

            // Checking if its a nightly build.
            CheckBuildType();

            // Determining total number of workers.
            Int32 numOfJobsTotal = NumOfLogProc + additionalWorkersNum;

            // Creating shuffled data for workers.
            InitRandomData(numOfJobsTotal);
            LogEvent("---------------------------------------------------------------");

            // Preparing database to run tests.
            SQLSelectPrepare();

            // Checking if we need to run parallel test only.
            if (SpecificTestType == LALSpecificTestType.LAL_PARALLEL_READ_ONLY_TEST)
            {
                LogEvent("--- Only parallel read-only test selected to be run...");

                // Doing read transactions performance with multiple workers.
                SQLSelectMulti(false, NumOfWorkers);

                // Indicating successful finish of the work.
                Console.WriteLine("LoadAndLatency parallel read-only test finished successfully!");

                return 0;
            }
            // Checking type of a test.
            else if (SpecificTestType == LALSpecificTestType.LAL_PARALLEL_UPDATES_TEST)
            {
                LogEvent("--- Only parallel updates test selected to be run...");

                // Doing update transactions performance.
                SQLSelectSingle(true);

                // Running simple multi-workers scalability test.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSimpleMultiTest(i, TotalNumOfObjectsInDB, 1);
                }

                // Running more complex multi-workers scalability test with updates.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSelectMulti(true, i);
                }

                // Running simple multi-workers scalability test.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSimpleMultiTest(i, TotalNumOfObjectsInDB / 100, 100);
                }

                // Indicating successful finish of the work.
                Console.WriteLine("LoadAndLatency parallel updates test finished successfully!");

                return 0;
            }

            // Testing that recreation/offset key works.
            // TODO: Reenable.
            //SQLTestOffsetKeySimple();
            //SQLTestOffsetKey();

            // First VS Foreach performance.
            FirstVsForeach();

            // Testing recreation key by selecting many rows.
            SQLSelectManyRows();

            // Doing read transactions performance with one worker.
            SQLSelectSingle(false);

            // Doing read transactions performance with multiple workers.
            SQLSelectMulti(false, NumOfWorkers);

            // Running ladder test if its a nightly build.
            if (TestLogger.IsNightlyBuild())
            {
                // Disabling logging through error console.
                TestLogger.TurnOffStatistics = true;

                // Running more complex multi-workers scalability test.
                for (Int32 i = 1; i <= NumOfWorkers; i++)
                {
                    SQLSelectMulti(false, i);
                }

                // Doing update transactions performance.
                SQLSelectSingle(true);

                // Running simple multi-workers scalability test.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSimpleMultiTest(i, TotalNumOfObjectsInDB, 1);
                }

                // Running more complex multi-workers scalability test with updates.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSelectMulti(true, i);
                }

                // Running simple multi-workers scalability test.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    SQLSimpleMultiTest(i, TotalNumOfObjectsInDB / 100, 100);
                }

                // Doing pyramidal test.
                for (Int32 i = MinNightlyWorkers; i <= NumOfWorkers; i++)
                {
                    LogEvent("Doing pyramidal test level " + i + "...");
                    for (SimpleObjectOperations testType = SimpleObjectOperations.SIMPLE_INITIAL_SHUFFLE;
                        testType <= SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY;
                        testType++)
                    {
                        SimplePerformanceTest(NumObjectsPerTransaction, TransactionsMagnifier * i / 5, testType, 0, true);
                    }
                }

                // Enabling logging through error console and important messages.
                TestLogger.TurnOffStatistics = false;
            }

            // Indicating successful finish of the work.
            Console.WriteLine("LoadAndLatency successfully finished!");

            return 0;
        }

        /// <summary>
        /// Prepare the database for running tests.
        /// </summary>
        void SQLSelectPrepare()
        {
            LogEvent("--- Preparing database to perform tests...");
            LogEvent("   Number of created objects in database: " + TotalNumOfObjectsInDB);

            // Purging the database if there are any objects in it.
            if (ThereAreObjectsInDB())
            {
                LogEvent("   Purging all existing objects...");
                using (Transaction transaction = new Transaction())
                {
                    transaction.Scope(() => {
                        //Int32 trans = 0;

                        foreach (TestClass t in Db.SQL("SELECT t FROM TestClass t")) {
                            t.Delete();

                            // Checking if we need to commit transaction.
                            /*trans++;
                            if (trans >= 1000)
                            {
                                trans = 0;
                                transaction.Commit();
                            }*/
                        }

                        // Committing the final transaction.
                        transaction.Commit();
                    });
                }
            }

            // Creating objects only if there are none of them.
            LogEvent(String.Format("   Inserting new {0} average-size objects using {1} transactions with {2} object inserts each.",
                TotalNumOfObjectsInDB, TransactionsMagnifier, NumObjectsPerTransaction));

            PreciseTimer perfTimer = new PreciseTimer();
            perfTimer.Start();

            // Populating the database with data.
            PopulateDatabase();

            perfTimer.Stop();

            // Printing info.
            Double averageObjInsertMcs = (perfTimer.DurationMcs / TotalNumOfObjectsInDB);
            LogEvent(String.Format("   {0} objects inserted with time of {1:N2} mcs each (totally took {2} mcs).",
                TotalNumOfObjectsInDB,
                averageObjInsertMcs.ToString(),
                perfTimer.DurationMcs));

            Int32 averageObjInsertNs = (Int32)(averageObjInsertMcs * 1000.0);

            if (startedOnClient)
                TestLogger.ReportStatistics("loadandlatency_medium_object_insert_within_big_transaction__nanoseconds_per_object_client", averageObjInsertNs);
            else
                TestLogger.ReportStatistics("loadandlatency_medium_object_insert_within_big_transaction__nanoseconds_per_object", averageObjInsertNs);
        }

        /// <summary>
        /// Runs single worker SQL test.
        /// </summary>
        void SQLSelectSingle(Boolean updatesOn)
        {
            LogEvent("---------------------------------------------------------------");
            LogEvent("   Running one scheduler SQL test with updates = " + updatesOn);

            // Running through all queries.
            for (Int32 q = 0; q < NumOfQueryTypes; q++)
            {
                // Cleaning timers.
                ResetUnsafeTimers();

                // Starting the test on one thread.
                WorkerParamClass workerParams = new WorkerParamClass(SingleWorkerSchedId, q, updatesOn);
                SQLTestWorker(workerParams);
            }

            LogEvent("---------------------------------------------------------------");
        }

        /// <summary>
        /// Compares performance between first and foreach.
        /// </summary>
        void FirstVsForeach()
        {
            LogEvent("---------------------------------------------------------------");

            // Re-Shuffling worker data.
            ReShuffleData(0);

            // FIRST.
            Int32 numFound = 0;

            PreciseTimer perfTimer = new PreciseTimer();
            perfTimer.Start();

            for (Int32 i = 0; i < TotalNumOfObjectsInDB; i++)
            {
                Db.Transact(delegate
                {
                    TestClass o = (TestClass)Db.SQL("SELECT c FROM TestClass c WHERE c.prop_int64 = ?", g_shuffledArrayInt64[0, i]).First;
                    if (o != null)
                        numFound++;
                });
            }

            perfTimer.Stop();

            LogEvent(String.Format("{0} FIRST transactions with {1} found objects took: {2} mcs.", TotalNumOfObjectsInDB, numFound, perfTimer.DurationMcs));

            // FOREACH.
            perfTimer.Reset();
            perfTimer.Start();

            numFound = 0;
            for (Int32 i = 0; i < TotalNumOfObjectsInDB; i++)
            {
                Db.Transact(delegate
                {
                    foreach (TestClass o in Db.SQL("SELECT c FROM TestClass c WHERE c.prop_int64 = ?", g_shuffledArrayInt64[0, i]))
                    {
                        if (o != null)
                            numFound++;
                    }
                });
            }

            perfTimer.Stop();

            LogEvent(String.Format("{0} FOREACH transactions with {1} found objects took: {2} mcs.", TotalNumOfObjectsInDB, numFound, perfTimer.DurationMcs));

            LogEvent("---------------------------------------------------------------");
        }

        /// <summary>
        /// Runs SQL test using offset key mechanisms.
        /// </summary>
        void SQLTestOffsetKeySimple()
        {
            // Offset key byte buffer.
            Byte[] offsetKey = null;

            // Starting some SQL query.
            using (SqlEnumerator<Object> sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c FETCH ?", 5).GetEnumerator())
            {
                for (Int32 i = 0; i < 5; i++)
                {
                    sqlEnum.MoveNext();
                    Console.Write(((TestClass)sqlEnum.Current).prop_int64 + " ");
                }

                // Fetching the recreation key.
                offsetKey = sqlEnum.GetOffsetKey();
                if (offsetKey == null)
                    throw new Exception("GetOffsetKey failed...");
            }

            // Now recreating the enumerator state using the offset key.
            Console.WriteLine();
            Console.WriteLine("Recreating the SQL enumerator...");
            using (SqlEnumerator<Object> sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c FETCH ? OFFSETKEY ?", 10, offsetKey).GetEnumerator())
            {
                for (Int32 i = 0; i < 10; i++)
                {
                    sqlEnum.MoveNext();
                    Console.Write(((TestClass)sqlEnum.Current).prop_int64 + " ");
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Runs SQL test using offset key mechanisms.
        /// </summary>
        void SQLTestOffsetKey()
        {
            LogEvent("---------------------------------------------------------------");
            LogEvent("   Running SQL SELECT recreation key test.");

            // Performance measure timer.
            PreciseTimer perfTimer = new PreciseTimer();
            perfTimer.Start();

            // Number of objects to fetch.
            Int32 fetchNumber = NumObjectsPerTransaction,
                repeats = 100;

            Random random = new Random((Int32)DateTime.Now.Ticks);

            // Offset key byte buffer.
            Byte[] offsetKey = null;

            // Running several repeats.
            for (Int32 u = 0; u < repeats; u++)
            {
                Int64 calcChecksum = 0, m = 0, artChecksum = 0;

                // Transaction scope.
                using (Transaction transaction = new Transaction())
                {
                    transaction.Scope(() => {
                        SqlEnumerator<Object> sqlEnum = null;

                        // Running throw all transactions.
                        for (Int32 trans = 0; trans < TransactionsMagnifier; trans++) {
                            if (trans == 0) {
                                sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c FETCH ?", fetchNumber).GetEnumerator();
                            } else {
                                if (!startedOnClient) {
                                    sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c FETCH ? OFFSETKEY ?", fetchNumber + 1, offsetKey).GetEnumerator();

                                    // Moving enumerator to the next position after recreation.
                                    sqlEnum.MoveNext();
                                } else {
                                    sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c FETCH ? OFFSETKEY ?", fetchNumber, offsetKey).GetEnumerator();
                                }
                            }

                            // Fetching only the first result and getting the object checksum.
                            for (Int32 k = 0; k < fetchNumber; k++) {
                                // Checking that results always exist.
                                if (!sqlEnum.MoveNext()) {
                                    // Throwing an exception that will cause test failure.
                                    String errMessage = "Object does not exist when it should.";
                                    Console.WriteLine(errMessage);
                                    throw new Exception(errMessage);
                                }

                                TestClass curObj = sqlEnum.Current as TestClass;

                                // Calculating object's checksum.
                                calcChecksum += curObj.GetCheckSum();

                                // Calculating artificial checksum.
                                artChecksum += m;
                                m++;

                                // Checking that checksums are the same.
                                if (calcChecksum != artChecksum) {
                                    // Throwing an exception that will cause test failure.
                                    String errMessage = "Inconsistent checksums: [" + artChecksum + ", " + calcChecksum + "].";
                                    Console.WriteLine(errMessage);
                                    throw new Exception(errMessage);
                                }
                            }

                            // Fetching the offset key.
                            offsetKey = sqlEnum.GetOffsetKey();
                            if (offsetKey == null)
                                throw new Exception("GetOffsetKey failed...");

                            // Checking that exactly needed amount of objects is fetched.
                            if (sqlEnum.MoveNext()) {
                                // Throwing an exception that will cause test failure.
                                String errMessage = "Unexpected object exists when it shouldn't.";
                                Console.WriteLine(errMessage);
                                throw new Exception(errMessage);
                            }

                            // Disposing the enumerator.
                            sqlEnum.Dispose();
                        }
                    });
                }
            }

            perfTimer.Stop();

            LogEvent(String.Format("   {0} rows selection {1} times, {2} repeats took: {3} mcs.", NumObjectsPerTransaction, TransactionsMagnifier, repeats, perfTimer.DurationMcs));
            LogEvent("---------------------------------------------------------------");
        }

        /// <summary>
        /// Runs SQL test for many rows SELECT.
        /// </summary>
        void SQLSelectManyRows()
        {
            LogEvent("---------------------------------------------------------------");

            // Number of objects to fetch.
            Int32 fetchNumber = NumObjectsPerTransaction,
                repeats = 100;

            LogEvent(String.Format("   Running SQL SELECT test for {0} rows selection (pyramide with FETCH), {1} times, {2} repeats.", NumObjectsPerTransaction, TransactionsMagnifier, repeats));

            // Creating indexes array strings.
            String[] shuffledArrayString = new String[TotalNumOfObjectsInDB];
            for (Int64 k = 0; k < TotalNumOfObjectsInDB; k++)
                shuffledArrayString[k] = k.ToString();

            // Performance measure timer.
            PreciseTimer perfTimer = new PreciseTimer();
            perfTimer.Start();

            // Running several repeats.
            for (Int32 u = 0; u < repeats; u++)
            {
                Int64 calcChecksum = 0, m = 0, artChecksum = 0;

                // Running specified number of transactions.
                for (Int32 i = 0; i < TransactionsMagnifier; i++)
                {
                    // Transaction scope.
                    using (Transaction transaction = new Transaction())
                    {
                        transaction.Scope(() => {
                            // Fetching the enumerator.
                            using (SqlEnumerator<Object> sqlEnum = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c WHERE c.prop_int64 >= ? FETCH ?", m, fetchNumber).GetEnumerator()) {
                                // Fetching only the first result and getting the object checksum.
                                for (Int32 k = 0; k < fetchNumber; k++) {
                                    // Checking that results always exist.
                                    if (!sqlEnum.MoveNext()) {
                                        // Throwing an exception that will cause test failure.
                                        String errMessage = "Object does not exist when it should.";
                                        Console.WriteLine(errMessage);
                                        throw new Exception(errMessage);
                                    }

                                    TestClass curObj = sqlEnum.Current as TestClass;

                                    // Calculating object's checksum.
                                    calcChecksum += curObj.GetCheckSum();

                                    // Calculating artificial checksum.
                                    artChecksum += m;
                                    m++;

                                    // Checking that checksums are the same.
                                    if (calcChecksum != artChecksum) {
                                        // Throwing an exception that will cause test failure.
                                        String errMessage = "Inconsistent checksums: [" + artChecksum + ", " + calcChecksum + "].";
                                        Console.WriteLine(errMessage);
                                        throw new Exception(errMessage);
                                    }
                                }

                                // Checking that exactly needed amount of objects is fetched.
                                if (sqlEnum.MoveNext()) {
                                    // Throwing an exception that will cause test failure.
                                    String errMessage = "Unexpected object exists when it shouldn't.";
                                    Console.WriteLine(errMessage);
                                    throw new Exception(errMessage);
                                }
                            }
                        });
                    }
                }
            }

            perfTimer.Stop();

            LogEvent(String.Format("   {0} rows selection {1} times, {2} repeats took: {3} mcs.", NumObjectsPerTransaction, TransactionsMagnifier, repeats, perfTimer.DurationMcs));
            LogEvent("---------------------------------------------------------------");
        }

        /// <summary>
        /// Runs parallel SQL test with multiple workers.
        /// </summary>
        void SQLSelectMulti(Boolean updatesOn, Int32 workers)
        {
            LogEvent("---------------------------------------------------------------");
            LogEvent("   Running parallel SQL test with " + workers + " workers and updates = " + updatesOn);

            // Testing all types.
            for (Int32 q = 0; q < NumOfQueryTypes; q++)
            {
                // Getting current time in ticks.
                UInt64 startTimeTicks = 0;
                QueryPerformanceCounter(out startTimeTicks);

                // Cleaning timers.
                ResetUnsafeTimers();

                // Starting all workers.
                for (Byte i = 0; i < workers; i++)
                {
                    // Creating worker parameters.
                    Byte schedulerId = (Byte)(i % NumOfLogProc);
                    WorkerParamClass workerParams = new WorkerParamClass(schedulerId, q, updatesOn);

                    // Checking if we are inside database.
                    if (!startedOnClient)
                    {
                        // Starting workers as Starcounter jobs.
                        Scheduling.ScheduleTask(() => SQLTestWorker(workerParams), false, schedulerId);
                    }
                    else
                    {
                        // Starting workers as several system threads.
                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(SQLSelectTest_Client), workerParams);
                    }
                }

                // Waiting for all threads to finish.
                while (g_numWorkersFinishedUnsafe < workers)
                    Thread.Sleep(5000);

                // Getting the maximum workers time in microseconds.
                Int64 timeElapsedMcs = (Int64)((g_endTimeTicksUnsafe - startTimeTicks) / timerFreqTicksPerMcs);

                // Calculating number of transactions measured from outside.
                Double outerTps = (1000000.0 / timeElapsedMcs) * TotalNumOfObjectsInDB * workers * NumOfRepeats;

                // Calculating number of transactions per machine per second.
                Int32 innerTps = (Int32)((1000000.0 / g_totalTimeMcsUnsafe) * TotalNumOfObjectsInDB * workers * workers * NumOfRepeats);

                // Determining type of transaction.
                String transType = "Read";
                if (updatesOn)
                    transType = "Update";

                // Logging results.
                LogEvent(String.Format("   Average number of {0} TPS for {1} using {2} parallel workers is {3} (totally took {4} mcs with outer TPS {5}).",
                    transType,
                    QueryStatDataString[q],
                    workers,
                    innerTps,
                    timeElapsedMcs,
                    outerTps));

                // Reporting to the statistics.
                ReportStatisticsValueForTransactions(innerTps, transType, QueryStatDataString[q]);
            }

            LogEvent("---------------------------------------------------------------");
        }

        void ReportStatisticsValueForType(Boolean useIndividualTransactions, Int32 nsPerOper, String transType, String dataType)
        {
            if (useIndividualTransactions)
            {
                if (startedOnClient)
                    TestLogger.ReportStatistics(
                        String.Format("loadandlatency_datatype_{0}_individual_{1}_transactions__nanoseconds_per_transaction_client", dataType.ToLower(), transType.ToLower()), nsPerOper);
                else
                    TestLogger.ReportStatistics(
                        String.Format("loadandlatency_datatype_{0}_individual_{1}_transactions__nanoseconds_per_transaction", dataType.ToLower(), transType.ToLower()), nsPerOper);
            }
            else
            {
                if (startedOnClient)
                    TestLogger.ReportStatistics(
                        String.Format("loadandlatency_datatype_{0}_within_big_{1}_transaction__nanoseconds_per_operation_client", dataType.ToLower(), transType.ToLower()), nsPerOper);
                else
                    TestLogger.ReportStatistics(
                        String.Format("loadandlatency_datatype_{0}_within_big_{1}_transaction__nanoseconds_per_operation", dataType.ToLower(), transType.ToLower()), nsPerOper);
            }
        }

        void ReportStatisticsValueForTransactions(Int32 numberOfTrans, String transType, String dataType)
        {
            if (startedOnClient)
                TestLogger.ReportStatistics(
                    String.Format("loadandlatency_datatype_{0}_individual_{1}_transactions__transactions_per_machine_client", dataType.ToLower(), transType.ToLower()), numberOfTrans);
            else
                TestLogger.ReportStatistics(
                    String.Format("loadandlatency_datatype_{0}_individual_{1}_transactions__transactions_per_machine", dataType.ToLower(), transType.ToLower()), numberOfTrans);
        }

        void SQLSelectTest_Client(Object workerParams)
        {
            // Special handling for client and multiple threads. Wrapping the call
            // to the test in a new session so that each thread is spread out on
            // different schedulers. This is needed since a session is bound to a 
            // specific scheduler and if no session is specified a default one is
            // used that is always connected to the same scheduler.
            //Db.Current.CreateSession();
            SQLTestWorker((WorkerParamClass)workerParams);
            //Db.Current.CloseSession();
        }

        // Shuffled data arrays for all workers.
        Int64[,] g_shuffledArrayInt64 = null;
        String[,] g_shuffledArrayString = null;
        String[,] g_shuffledArrayStringLike = null;
        DateTime[,] g_shuffledArrayDateTime = null;
        Decimal[,] g_shuffledArrayDecimal = null;

        // Simple test workers data.
        Int64[] g_simpleTestRandInt64 = null;
        String[] g_simpleTestRandStrings = null;

        String[] g_simpleTestSavedStrings = null;
        Int64[] g_simpleTestSavedInt64 = null;
        SimpleObject[] g_simpleTestSavedObjects = null;

        /// <summary>
        /// Initializes random data for all workers.
        /// </summary>
        void InitRandomData(Int32 maxWorkers)
        {
            if (g_shuffledArrayInt64 != null)
                throw new Exception("Workers data was already initialized!");

            // Allocating data for workers.
            g_shuffledArrayInt64 = new Int64[maxWorkers, TotalNumOfObjectsInDB];
            g_shuffledArrayString = new String[maxWorkers, TotalNumOfObjectsInDB];
            g_shuffledArrayStringLike = new String[maxWorkers, TotalNumOfObjectsInDB];
            g_shuffledArrayDateTime = new DateTime[maxWorkers, TotalNumOfObjectsInDB];
            g_shuffledArrayDecimal = new Decimal[maxWorkers, TotalNumOfObjectsInDB];

            // Simple test workers data.
            Int32 totalWorkersObjects = maxWorkers * TotalNumOfObjectsInDB;
            g_simpleTestRandInt64 = new Int64[totalWorkersObjects];
            g_simpleTestSavedStrings = new String[totalWorkersObjects];
            g_simpleTestSavedInt64 = new Int64[totalWorkersObjects];
            g_simpleTestRandStrings = new String[totalWorkersObjects];
            g_simpleTestSavedObjects = new SimpleObject[totalWorkersObjects];

            // Processing all workers.
            for (Int32 workerId = 0; workerId < maxWorkers; workerId++)
            {
                // Filling up whole range with linear indexes.
                for (Int64 k = 0; k < TotalNumOfObjectsInDB; k++)
                    g_shuffledArrayInt64[workerId, k] = k;

                // Shuffling the indexes.
                Random random = new Random((Int32)(DateTime.Now.Ticks * (workerId + 1)));
                for (Int64 k = 0; k < TotalNumOfObjectsInDB; k++)
                {
                    // Generating random index.
                    Int32 randIndex = random.Next((Int32)TotalNumOfObjectsInDB);

                    // Switching two array elements.
                    Int64 savedValue = g_shuffledArrayInt64[workerId, k];
                    g_shuffledArrayInt64[workerId, k] = g_shuffledArrayInt64[workerId, randIndex];
                    g_shuffledArrayInt64[workerId, randIndex] = savedValue;
                }

                // Applying randomness to other data types.
                for (Int64 k = 0; k < TotalNumOfObjectsInDB; k++)
                {
                    g_shuffledArrayString[workerId, k] = g_shuffledArrayInt64[workerId, k].ToString();
                    g_shuffledArrayStringLike[workerId, k] = g_shuffledArrayInt64[workerId, k].ToString() + "%";
                    g_shuffledArrayDateTime[workerId, k] = new DateTime(g_shuffledArrayInt64[workerId, k]);
                    g_shuffledArrayDecimal[workerId, k] = (Decimal)g_shuffledArrayInt64[workerId, k];
                }
            }
        }

        /// <summary>
        /// Randomly reshuffles existing data.
        /// </summary>
        void ReShuffleData(Int32 workerId)
        {
            if (g_shuffledArrayInt64 == null)
                throw new Exception("Workers data was not initialized!");

            // Shuffling the indexes.
            Random random = new Random((Int32)(DateTime.Now.Ticks * (workerId + 1)));
            for (Int64 k = 0; k < TotalNumOfObjectsInDB; k++)
            {
                // Generating random index.
                Int32 randIndex = random.Next((Int32)TotalNumOfObjectsInDB);

                // Integer.
                Int64 oldInt = g_shuffledArrayInt64[workerId, k];
                g_shuffledArrayInt64[workerId, k] = g_shuffledArrayInt64[workerId, randIndex];
                g_shuffledArrayInt64[workerId, randIndex] = oldInt;

                // String.
                String oldString = g_shuffledArrayString[workerId, k];
                g_shuffledArrayString[workerId, k] = g_shuffledArrayString[workerId, randIndex];
                g_shuffledArrayString[workerId, randIndex] = oldString;

                // String Like.
                String oldStringLike = g_shuffledArrayStringLike[workerId, k];
                g_shuffledArrayStringLike[workerId, k] = g_shuffledArrayStringLike[workerId, randIndex];
                g_shuffledArrayStringLike[workerId, randIndex] = oldStringLike;

                // DateTime.
                DateTime oldDateTime = g_shuffledArrayDateTime[workerId, k];
                g_shuffledArrayDateTime[workerId, k] = g_shuffledArrayDateTime[workerId, randIndex];
                g_shuffledArrayDateTime[workerId, randIndex] = oldDateTime;

                // Decimal.
                Decimal oldDecimal = g_shuffledArrayDecimal[workerId, k];
                g_shuffledArrayDecimal[workerId, k] = g_shuffledArrayDecimal[workerId, randIndex];
                g_shuffledArrayDecimal[workerId, randIndex] = oldDecimal;
            }
        }

        /// <summary>
        /// Core SQL test worker procedure.
        /// </summary>
        void SQLTestWorker(Object paramsForWorker)
        {
            // Casting worker parameters.
            WorkerParamClass workerParams = (WorkerParamClass)paramsForWorker;

            // Determining the scheduler number for this test.
            Byte schedulerId = workerParams.SchedulerId;
            Int32 queryId = workerParams.QueryId;

            // Data id for the scheduler.
            Int32 workerId = schedulerId;
            if (schedulerId == SingleWorkerSchedId)
                workerId = 0;

            // Re-Shuffling workers data.
            ReShuffleData(workerId);

            // Performance measure timer.
            PreciseTimer perfTimer = new PreciseTimer();

            // Running several times.
            if (schedulerId == SingleWorkerSchedId)
                LogEvent("--- Running test for the query: " + QueryStrings[queryId]);

            // For different use of transactions.
            Boolean[] indivTrans = { true, false };
            for (Int32 t = 0; t < 2; t++)
            {
                // Printing info if we are running only one scheduler test.
                if (schedulerId == SingleWorkerSchedId)
                {
                    if (indivTrans[t])
                        LogEvent("   With Separate transaction per each SELECT: ");
                    else
                        LogEvent("   With one Big transaction for all SELECTs: ");
                }
                PrepareSelectQuery((QueryDataTypes)queryId, workerId);
                // Starting new time measure.
                perfTimer.Reset();
                perfTimer.Start();

                // Repeating same test several times.
                for (Int32 i = 0; i < NumOfRepeats; i++)
                {
                    // Running the core performance test function.
                    PerformanceTestPerQueryType(
                        (QueryDataTypes)queryId,
                        indivTrans[t],
                        workerParams.UpdatesOn,
                        workerId);
                }

                perfTimer.Stop();

                // Calculating microseconds spent.
                Double testTimeMcs = perfTimer.DurationMcs;

                // Accumulating time results.
                lock (QueryStrings)
                {
                    g_totalTimeMcsUnsafe += testTimeMcs;
                    QueryPerformanceCounter(out g_endTimeTicksUnsafe);
                }

                // Printing the results.
                Double mcsPerOper = testTimeMcs / (TotalNumOfObjectsInDB * NumOfRepeats);

                // Printing performance results.
                if (schedulerId == SingleWorkerSchedId)
                {
                    // Determining type of transaction.
                    String transType = "Read";
                    if (workerParams.UpdatesOn)
                        transType = "Update";

                    // Logging the results.
                    LogEvent(String.Format("One {0} on average took {1:N2} mcs (totally took {2} mcs).",
                        transType,
                        mcsPerOper,
                        testTimeMcs / NumOfRepeats));

                    // Reporting to statistics.
                    Int32 nsPerOper = (Int32)(mcsPerOper * 1000.0);
                    ReportStatisticsValueForType(indivTrans[t], nsPerOper, transType, QueryStatDataString[queryId]);
                }
                else
                {
                    // LogEvent(String.Format("One SELECT transaction with query '{0}' on VP {1} on average took {2:N2} mcs.", QueryStrings[q], schedId, mcsPerSelect));

                    // For parallel test, doing only using individual transactions.
                    break;
                }
            }

            // Indicating that this worker has finished.
            lock (QueryStrings) { g_numWorkersFinishedUnsafe++; }

            // Status.
            //Console.WriteLine("Worker thread " + schedId + " finished! Number of finished threads: " + numThreadsFinishedUnsafe);
        }

        /// <summary>
        /// Runs parallel SQL simple tests.
        /// </summary>
        void SQLSimpleMultiTest(
            Int32 workers,
            Int32 numTransactionsPerWorker,
            Int32 numObjectsPerEachTransaction)
        {
            LogEvent("---------------------------------------------------------------");
            LogEvent("   Running parallel simple SQL tests with " + workers + " workers.");

            // Running throw all types of simple tests except PURGE.
            for (SimpleObjectOperations testType = SimpleObjectOperations.SIMPLE_INITIAL_SHUFFLE;
                testType <= SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY;
                testType++)
            {
                // Getting current time in ticks.
                UInt64 startTimeTicks = 0;
                QueryPerformanceCounter(out startTimeTicks);

                // Cleaning timers.
                ResetUnsafeTimers();

                // Starting all workers.
                for (Byte i = 0; i < workers; i++)
                {
                    SimpleTestParamClass workerParams = new SimpleTestParamClass(
                        i * numTransactionsPerWorker * numObjectsPerEachTransaction,
                        testType,
                        numTransactionsPerWorker,
                        numObjectsPerEachTransaction);

                    // Checking if we are on server.
                    if (!startedOnClient)
                    {
                        // Starting workers as Starcounter jobs.
                        Scheduling.ScheduleTask(() => SQLSimpleMultiTestWrapper(workerParams), false, (Byte)(i % NumOfLogProc));
                    }
                    else
                    {
                        // Starting workers as several system threads.
                        System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(SQLSimpleMultiTestWrapper_Client), workerParams);
                    }
                }

                // Waiting for all threads to finish.
                while (g_numWorkersFinishedUnsafe < workers)
                    Thread.Sleep(5000);

                lock (QueryStrings)
                {
                    // Checking if any statistics was retrieved during the workers run.
                    if (g_nsPerObjectUnsafe > 0)
                    {
                        // Repeating same test several times.
                        Int32 numOfRepeats = NumOfRepeats;

                        // Not repeating if adding/removing data.
                        if ((testType == SimpleObjectOperations.SIMPLE_OBJECT_INSERT) ||
                            (testType == SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY))
                        {
                            numOfRepeats = 1;
                        }

                        // Getting the maximum workers time in microseconds.
                        Int64 timeElapsedMcs = (Int64)((g_endTimeTicksUnsafe - startTimeTicks) / timerFreqTicksPerMcs);

                        // Calculating number of transactions measured from outside.
                        Double outerTps = (1000000.0 / timeElapsedMcs) * numTransactionsPerWorker * workers * numOfRepeats;

                        // Calculating inner OPS per operation.
                        Double innerOps = (1000000000.0 * workers * workers) / g_nsPerObjectUnsafe;

                        // Printing the results.
                        LogEvent(String.Format("OPS (each worker does {0} transactions with {1} objects each) for type {2} for {3} workers is {4} (totally took {5} mcs with outer TPS {6}).",
                            numTransactionsPerWorker,
                            numObjectsPerEachTransaction,
                            testType,
                            workers,
                            innerOps,
                            timeElapsedMcs,
                            outerTps));
                    }
                }
            }

            LogEvent("---------------------------------------------------------------");
        }

        /// <summary>
        /// Wrapper around worker instance.
        /// </summary>
        void SQLSimpleMultiTestWrapper(Object param)
        {
            SimpleTestParamClass testParams = (SimpleTestParamClass)param;
            SimplePerformanceTest(testParams.NumTransactions, testParams.NumObjectsPerTransaction, testParams.TestType, testParams.WorkerGlobalOffset, false);
        }

        /// <summary>
        /// Just a client wrapper.
        /// </summary>
        void SQLSimpleMultiTestWrapper_Client(Object param)
        {
            //Db.Current.CreateSession();
            SQLSimpleMultiTestWrapper(param);
            //Db.Current.CloseSession();
        }

        // Simple data shuffle.
        void SimpleShuffle(Int32 numTransactions, Int32 objectsPerTransaction, Int32 startIndex)
        {
            // Shuffling simple data.
            Random random = new Random((Int32)(DateTime.Now.Ticks));
            Int32 objIndex = startIndex, numObjects = numTransactions * objectsPerTransaction;
            for (Int32 k = 0; k < numObjects; k++)
            {
                // Generating random index.
                Int32 randIndex = random.Next(numObjects) + startIndex;

                // Switching two array elements.
                Int64 savedValueInt64 = g_simpleTestRandInt64[objIndex];
                g_simpleTestRandInt64[objIndex] = g_simpleTestRandInt64[randIndex];
                g_simpleTestRandInt64[randIndex] = savedValueInt64;

                String savedValueString = g_simpleTestRandStrings[objIndex];
                g_simpleTestRandStrings[objIndex] = g_simpleTestRandStrings[randIndex];
                g_simpleTestRandStrings[randIndex] = savedValueString;

                objIndex++;
            }
        }

        // Performance test for simple object insert/update/delete.
        void SimplePerformanceTest(Int32 numTransactions, Int32 objectsPerTransaction, SimpleObjectOperations typeOfOperation, Int32 startIndex, Boolean printStuff)
        {
            // Determining the operation type.
            String operString = typeOfOperation.ToString();
            switch (typeOfOperation)
            {
                    case SimpleObjectOperations.SIMPLE_CACHE_QUERIES:
                    {
                        // Executing query.
                        SimpleObject o = Db.SQL < SimpleObject>(SimpleSelectIntQuery, startIndex).First;

                        // Indicating that this worker has finished.
                        lock (QueryStrings) { g_numWorkersFinishedUnsafe++; }
                        return;
                    }

                    case SimpleObjectOperations.SIMPLE_INITIAL_SHUFFLE:
                    {
                        Random random = new Random((Int32)(DateTime.Now.Ticks));
                        Int32 objIndex = startIndex, numObjects = numTransactions * objectsPerTransaction;

                        // Initializing simple data.
                        for (Int32 i = 0; i < numObjects; i++)
                        {
                            g_simpleTestRandInt64[objIndex] = objIndex;
                            objIndex++;
                        }

                        // Shuffling simple data.
                        objIndex = startIndex;
                        for (Int32 i = 0; i < numObjects; i++)
                        {
                            // Generating random index.
                            Int32 randIndex = random.Next(numObjects) + startIndex;

                            // Switching two array elements.
                            Int64 savedValue = g_simpleTestRandInt64[objIndex];
                            g_simpleTestRandInt64[objIndex] = g_simpleTestRandInt64[randIndex];
                            g_simpleTestRandInt64[randIndex] = savedValue;

                            // Applying randomness to other elements.
                            g_simpleTestRandStrings[objIndex] = g_simpleTestRandInt64[objIndex] + "_rand";
                            objIndex++;
                        }

                        // Indicating that this worker has finished.
                        lock (QueryStrings) { g_numWorkersFinishedUnsafe++; }
                        return;
                    }

                    case SimpleObjectOperations.SIMPLE_OBJECTS_PURGE:
                    {
                        using (Transaction transaction = new Transaction())
                        {
                            transaction.Scope(() => {
                                //Int32 trans = 0;

                                foreach (SimpleObject p in Db.SQL("SELECT s FROM SimpleObject s")) {
                                    p.Delete();

                                    // Checking if we need to commit transaction.
                                    /*trans++;
                                    if (trans >= 1000)
                                    {
                                        trans = 0;
                                        transaction.Commit();
                                    }*/
                                }

                                // Committing transaction.
                                transaction.Commit();
                            });
                        }

                        // Indicating that this worker has finished.
                        lock (QueryStrings) { g_numWorkersFinishedUnsafe++; }
                        return;
                    }
            }

            // Describing the following test.
            if (printStuff)
            {
                LogEvent(String.Format("--- Starting simple object {0} performance test: {1} objects, {2} transactions with {3} objects each.",
                    operString, numTransactions * objectsPerTransaction, numTransactions, objectsPerTransaction));
            }

            // Performance measure timer.
            PreciseTimer perfTimer = new PreciseTimer();

            // Used to track correct number of objects.
            Int32 objectsCounter = 0;

            // Repeating same test several times.
            Int32 numOfRepeats = NumOfRepeats;

            // Not repeating if adding/removing data.
            if ((typeOfOperation == SimpleObjectOperations.SIMPLE_OBJECT_INSERT) ||
                (typeOfOperation == SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY))
            {
                numOfRepeats = 1;
            }

            // Repeating the test several times.
            for (Int32 r = 0; r < numOfRepeats; r++)
            {
                // Reshuffling the data.
                SimpleShuffle(numTransactions, objectsPerTransaction, startIndex);

                // Starting the timer.
                perfTimer.Start();

                // Setting object counter to the range start.
                objectsCounter = startIndex;

                // Doing certain amount of objects per transaction.
                var transactions = new List<Task>();
                for (Int32 t = 0; t < numTransactions; t++)
                {
                    var res = RunSimplePerformanceTest(
                        typeOfOperation, 
                        objectsPerTransaction, 
                        objectsCounter
                    );

                    objectsCounter = res.Item1;
                    transactions.AddRange(res.Item2);
                }

                Task.WaitAll(transactions.ToArray());

                // Pausing the timer.
                perfTimer.Stop();
            }

            // Saving current time.
            lock (QueryStrings) { QueryPerformanceCounter(out g_endTimeTicksUnsafe); }

            // Elapsed microseconds without repeats.
            Double elapsedMcs = perfTimer.DurationMcs / numOfRepeats;

            // Checking correct number of processed objects.
            Int32 correctNumObjects = numTransactions * objectsPerTransaction;
            if (objectsCounter != (startIndex + correctNumObjects))
                throw new Exception(String.Format("Wrong number of processed objects: {0} out of {1}.", objectsCounter - startIndex, correctNumObjects));

            // Nanoseconds taken per simple object.
            Int32 nsPerSimpleObj = (Int32)((1000.0 * elapsedMcs) / correctNumObjects);

            // Printing statistics.
            if (printStuff)
            {
                LogEvent(String.Format("   One simple object {0} took {1:N2} ns (totally took {2} mcs).",
                    operString,
                    nsPerSimpleObj.ToString(),
                    elapsedMcs));

                if (startedOnClient)
                    TestLogger.ReportStatistics(String.Format("loadandlatency_{0}_{1}_transactions_with_{2}_objects_each__nanoseconds_per_object_client", operString, numTransactions, objectsPerTransaction), nsPerSimpleObj);
                else
                    TestLogger.ReportStatistics(String.Format("loadandlatency_{0}_{1}_transactions_with_{2}_objects_each__nanoseconds_per_object", operString, numTransactions, objectsPerTransaction), nsPerSimpleObj);
            }

            // Adding to global counter.
            lock (QueryStrings)
            {
                g_nsPerObjectUnsafe += nsPerSimpleObj;

                // Indicating that this worker has finished.
                g_numWorkersFinishedUnsafe++;
            }
        }

        private Tuple<Int32, IEnumerable<Task>> RunSimplePerformanceTest(SimpleObjectOperations typeOfOperation, Int32 objectsPerTransaction, Int32 objectsCounter) {
            var transactions = new List<Task>();
            switch (typeOfOperation) {
                // Simple insert.
                case SimpleObjectOperations.SIMPLE_OBJECT_INSERT: {
                        var tran = Db.TransactAsync(() =>
                        {
                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                                // Creating simple objects.
                                SimpleObject insObj = new SimpleObject(objectsCounter);
                                objectsCounter++;
                            }
                        });

                        transactions.Add(tran);

                        break;
                    }

                // Simple read attribute with query.
                case SimpleObjectOperations.SIMPLE_ATTR_STR_READ_WITH_QUERY: {
                        Db.Transact(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                                Int64 objIndex = g_simpleTestRandInt64[objectsCounter];

                            // Executing query.
                            SimpleObject o = Db.SQL<SimpleObject>(SimpleSelectIntQuery, objIndex).First;

                            // Reading string attribute.
                            g_simpleTestSavedStrings[objIndex] = o.updateString;

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });

                        break;
                    }

                // Simple read attribute with query.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_READ_WITH_QUERY: {
                        Db.Transact(() =>
                        {
                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                                Int64 objIndex = g_simpleTestRandInt64[objectsCounter];

                            // Executing query.
                            SimpleObject o = Db.SQL<SimpleObject>(SimpleSelectIntQuery, objIndex).First;

                            // Reading integer attribute.
                            g_simpleTestSavedInt64[objIndex] = o.fetchInt;

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });

                        break;
                    }

                // Simple update attribute with query.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_WITH_QUERY:
                case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_WITH_QUERY_PLUS_SNAPSHOT_ISOLATION: {
                        var tran = Db.TransactAsync(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                                Int64 objIndex = g_simpleTestRandInt64[objectsCounter];

                            // Executing query.
                            SimpleObject o = Db.SQL<SimpleObject>(SimpleSelectIntQuery, objIndex).First;

                            // Updating integer attribute.
                            o.updateInt = g_simpleTestRandInt64[objIndex];

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });


                        transactions.Add(tran);

                        break;
                    }

                // Simple update attribute with query.
                case SimpleObjectOperations.SIMPLE_ATTR_STR_UPDATE_WITH_QUERY:
                case SimpleObjectOperations.SIMPLE_ATTR_STR_UPDATE_WITH_QUERY_PLUS_SNAPSHOT_ISOLATION: {
                        var tran = Db.TransactAsync(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                                Int64 objIndex = g_simpleTestRandInt64[objectsCounter];

                            // Executing query.
                            SimpleObject o = Db.SQL<SimpleObject>(SimpleSelectIntQuery, objIndex).First;

                            // Updating string attribute.
                            o.updateString = g_simpleTestRandStrings[objIndex];

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });
                        transactions.Add(tran);
                        break;
                    }

                // Get objects for later use.
                case SimpleObjectOperations.SIMPLE_SAVE_OBJECT_WITH_QUERY: {
                        Db.Transact(() =>
                        { 
                            for (Int32 i = 0; i < objectsPerTransaction; i++) {
                                Int64 objIndex = g_simpleTestRandInt64[objectsCounter];

                                // Executing query.
                                SimpleObject o = Db.SQL<SimpleObject>(SimpleSelectIntQuery, objIndex).First;

                                // Saving object for later use.
                                g_simpleTestSavedObjects[objIndex] = o;

                                // Incrementing object counter.
                                objectsCounter++;
                            }
                        });

                        break;
                    }

                // Simple read object string attribute from saved object.
                case SimpleObjectOperations.SIMPLE_ATTR_STR_READ_FROM_SAVED_OBJECT: {
                        Db.Transact(() =>
                        {
                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                            // Reading attribute from saved object.
                            g_simpleTestSavedStrings[objectsCounter] = g_simpleTestSavedObjects[g_simpleTestRandInt64[objectsCounter]].updateString;

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });

                        break;
                    }

                // Simple read object integer attribute from saved object.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_READ_FROM_SAVED_OBJECT: {
                        Db.Transact(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                            // Reading attribute from saved object.
                            g_simpleTestSavedInt64[objectsCounter] = g_simpleTestSavedObjects[g_simpleTestRandInt64[objectsCounter]].fetchInt;

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });

                        break;
                    }

                // Simple update integer attribute in saved object.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_IN_SAVED_OBJECT:
                case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_IN_SAVED_OBJECT_PLUS_SNAPSHOT_ISOLATION: {
                        var tran = Db.TransactAsync(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                            // Writing attribute in saved object.
                            g_simpleTestSavedObjects[objectsCounter].updateInt = g_simpleTestRandInt64[g_simpleTestRandInt64[objectsCounter]];

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });

                        transactions.Add(tran);
                        break;
                    }

                // Simple update string attribute in saved object.
                case SimpleObjectOperations.SIMPLE_ATTR_STR_UPDATE_IN_SAVED_OBJECT:
                case SimpleObjectOperations.SIMPLE_ATTR_STR_UPDATE_IN_SAVED_OBJECT_PLUS_SNAPSHOT_ISOLATION: {
                        var tran = Db.TransactAsync(() =>
                        {

                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                            {
                            // Writing attribute in saved object.
                            g_simpleTestSavedObjects[objectsCounter].updateString = g_simpleTestRandStrings[g_simpleTestRandInt64[objectsCounter]];

                            // Incrementing object counter.
                            objectsCounter++;
                            }
                        });
                        transactions.Add(tran);
                        break;
                    }

                // Simple delete, update, update to the same value.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_WITH_RANGE_QUERY:
                case SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY:
                case SimpleObjectOperations.SIMPLE_ATTR_STR_INT_UPDATE_SAME_WITH_RANGE_QUERY: {
                        var tran = Db.TransactAsync(() =>
                        {

                            using (SqlEnumerator<Object> sqlResult = (SqlEnumerator<Object>)Db.SQL("SELECT s FROM SimpleObject s WHERE s.fetchInt >= ? AND s.fetchInt < ?",
                                objectsCounter, objectsCounter + objectsPerTransaction).GetEnumerator())
                            {
                                Int64 realCheckSum = 0, artCheckSum = 0;

                            // Processing each object from a range.
                            for (Int32 i = 0; i < objectsPerTransaction; i++)
                                {
                                // Trying to get an object.
                                if (!sqlResult.MoveNext())
                                        throw new Exception(String.Format("Object does not exist where it should: {0}.", objectsCounter));

                                // Current database object.
                                SimpleObject obj = (sqlResult.Current as SimpleObject);

                                // Getting checksum.
                                realCheckSum += obj.FetchInt();

                                // Determining operation.
                                switch (typeOfOperation)
                                    {
                                        case SimpleObjectOperations.SIMPLE_ATTR_INT_UPDATE_WITH_RANGE_QUERY:
                                            obj.UpdateInt();
                                            break;

                                        case SimpleObjectOperations.SIMPLE_OBJECT_DELETE_WITH_QUERY:
                                            obj.Delete();
                                            break;

                                        case SimpleObjectOperations.SIMPLE_ATTR_STR_INT_UPDATE_SAME_WITH_RANGE_QUERY:
                                            obj.UpdateSameStringInt();
                                            break;

                                        default:
                                            throw new Exception("Incorrect operation type.");
                                    }

                                // Calculating artificial checksum.
                                artCheckSum += objectsCounter;

                                // Incrementing object counter.
                                objectsCounter++;
                                }

                            // Checking that we can't fetch anymore objects.
                            if (sqlResult.MoveNext())
                                    throw new Exception("Object fetched where it should not.");

                            // Checking correct checksums.
                            if (artCheckSum != realCheckSum)
                                    throw new Exception(String.Format("Incorrect checksums: {0} vs {1}.", realCheckSum, artCheckSum));
                            }
                        });
                        transactions.Add(tran);
                        break;
                    }

                // Simple shadow update.
                case SimpleObjectOperations.SIMPLE_ATTR_INT_SHADOW_UPDATE_WITH_QUERY: {
                        Int32 objectsCounterSaved = objectsCounter;

                        // Running some times.
                        for (Int32 c = 0; c < 2; c++) {
                            var tran = Db.TransactAsync(() =>
                            {

                                // Resetting object counter.
                                objectsCounter = objectsCounterSaved;

                                SqlEnumerator<Object> sqlResult = null;
                                Boolean firstRound = ((c % 2) == 0);

                                try
                                {
                                    // Printing all objects.
                                    foreach (SimpleObject p in Db.SQL("SELECT s FROM SimpleObject s"))
                                    {
                                        Console.Write(p.FetchInt() + " ");
                                    }
                                    Console.WriteLine();

                                    // Determining query type.
                                    if (firstRound)
                                        sqlResult = (SqlEnumerator<Object>)Db.SQL("SELECT s FROM SimpleObject s WHERE s.fetchInt >= ? AND s.fetchInt < ?", objectsCounter, objectsCounter + objectsPerTransaction).GetEnumerator();
                                    else
                                        sqlResult = (SqlEnumerator<Object>)Db.SQL("SELECT s FROM SimpleObject s WHERE s.fetchInt <= ? AND s.fetchInt > ?", -objectsCounter, -objectsCounter - objectsPerTransaction).GetEnumerator();

                                    Int64 realCheckSum = 0, artCheckSum = 0;

                                    for (Int32 i = 0; i < objectsPerTransaction; i++)
                                    {
                                        // Getting an object.
                                        if (!sqlResult.MoveNext())
                                            throw new Exception(String.Format("Object does not exist where it should: {0}.", objectsCounter));

                                        // Current database object.
                                        SimpleObject obj = (sqlResult.Current as SimpleObject);

                                        // Getting checksum.
                                        if (firstRound)
                                            realCheckSum += obj.FetchInt();
                                        else
                                            realCheckSum -= obj.FetchInt();

                                        // Performing reverse.
                                        obj.UpdateIntShadow();

                                        // Calculating artificial checksum.
                                        artCheckSum += objectsCounter;

                                        // Incrementing object counter.
                                        objectsCounter++;
                                    }

                                    // Checking that we can't fetch anymore objects.
                                    if (sqlResult.MoveNext())
                                        throw new Exception("Object fetched where it should not.");

                                    // Checking correct checksums.
                                    if (artCheckSum != realCheckSum)
                                        throw new Exception(String.Format("Incorrect checksums: {0} vs {1}.", realCheckSum, artCheckSum));
                                }
                                finally
                                {
                                    sqlResult.Dispose();
                                }
                            });
                            transactions.Add(tran);
                        }

                        break;
                    }

                default:
                    throw new Exception("Wrong operation type.");
            }

            return Tuple.Create(objectsCounter, transactions.AsEnumerable() );
        }

        /// <summary>
        /// Check if objects already exist in DB.
        /// </summary>
        Boolean ThereAreObjectsInDB()
        {
            bool objectsExists = false; 

            using (Transaction transaction = new Transaction())
            {
                transaction.Scope(() => {
                    SqlEnumerator<Object> sqlResult = null;
                    try {
                        sqlResult = (SqlEnumerator<Object>)Db.SQL("SELECT c FROM TestClass c").GetEnumerator();

                        // If there are instances in the database, consider the example
                        // data already created.
                        // Hence, calling this method each time in the exercise does
                        // not duplicate the data set.
                        Int64 objCount = 0;
                        while (sqlResult.MoveNext())
                            objCount++;

                        if (objCount > 0) {
                            LogEvent(String.Format("   {0} objects already exist in a database.", objCount));
                            objectsExists = true;
                        }
                    } finally {
                        if (sqlResult != null)
                            sqlResult.Dispose();
                    }
                });
                return objectsExists;
            }
        }

        void PrepareSelectQuery(QueryDataTypes queryType, Int32 workerId) {
            Object sqlParamObj = null;
            switch (queryType) {
                case QueryDataTypes.DATA_INTEGER: {
                        sqlParamObj = g_shuffledArrayInt64[workerId, 0];
                        break;
                    }

                case QueryDataTypes.DATA_STRING: {
                        sqlParamObj = g_shuffledArrayString[workerId, 0];
                        break;
                    }

                case QueryDataTypes.DATA_DATETIME: {
                        sqlParamObj = g_shuffledArrayDateTime[workerId, 0];
                        break;
                    }

                case QueryDataTypes.DATA_STRING_LIKE: {
                        sqlParamObj = g_shuffledArrayStringLike[workerId, 0];
                        break;
                    }

                case QueryDataTypes.DATA_STRING_STARTS_WITH: {
                        sqlParamObj = g_shuffledArrayString[workerId, 0];
                        break;
                    }

                case QueryDataTypes.DATA_DECIMAL: {
                        sqlParamObj = g_shuffledArrayDecimal[workerId, 0];
                        break;
                    }
            }
            SqlEnumerator<Object> sqlEnum = (SqlEnumerator<Object>)Db.SQL(QueryStrings[(Int32)queryType], sqlParamObj).GetEnumerator();
        }

        /// <summary>
        /// Performs SELECT and if needed UPDATE for a given query type.
        /// </summary>
        void PerformanceTestPerQueryType(
            QueryDataTypes queryType,
            Boolean useIndividualTransactions,
            Boolean performUpdate,
            Int32 workerId)
        {
            if (!useIndividualTransactions) {
                var trans = new Transaction();
                trans.Scope<QueryDataTypes, Boolean, Boolean, Int32>(RunPerformanceTestPerQueryType, queryType, useIndividualTransactions, performUpdate, workerId);

                // Committing if we have updates.
                if (performUpdate)
                    CommitWithRetries(trans, TransRetriesNum);

                // Disposing the transaction.
                trans.Dispose();
            } else {
                RunPerformanceTestPerQueryType(queryType, useIndividualTransactions, performUpdate, workerId);
            }

            // Printing profile results at the end of each test.
            //Application.Profiler.DrawResults();
            //if (startedOnClient)
            //    Application.Profiler.DrawResultsServer();
        }

        private class TransactionRollbackException : System.Exception
        {
        }

        private void RunPerformanceTestPerQueryType(QueryDataTypes queryType, Boolean useIndividualTransactions, Boolean performUpdate, Int32 workerId) {

            var transactions = new List<System.Threading.Tasks.Task>();
            // Running each SELECT in separate transaction.
            const int transaction_in_group_limit = 10000;


            for (Int64 i = 0; i < TotalNumOfObjectsInDB; i++) {
                if (useIndividualTransactions) {

                    try
                    {
                        transactions.Add(
                            Db.TransactAsync(() =>
                            {
                                RunOnePerformanceTestPerQueryType(queryType, performUpdate, workerId, i);

                                if (!performUpdate)
                                    throw new TransactionRollbackException();
                            }, 
                            0, new Db.Advanced.TransactOptions { maxRetries = TransRetriesNum }));
                    }
                    catch (TransactionRollbackException) { }
                } else {
                    RunOnePerformanceTestPerQueryType(queryType, performUpdate, workerId, i);
                }

                if ( (i+1)%transaction_in_group_limit == 0)
                {
                    System.Threading.Tasks.Task.WaitAll(transactions.ToArray());
                    transactions.Clear();
                }
            }

            System.Threading.Tasks.Task.WaitAll(transactions.ToArray());
        }

        private void RunOnePerformanceTestPerQueryType(QueryDataTypes queryType, Boolean performUpdate, Int32 workerId, Int64 i) {
            Int64 calcChecksum = 0, artChecksum = 0;

            //Application.Profiler.Start("Time for Profiler Overhead Estimator.", 13);
            //Application.Profiler.Stop(13);

            // Object representing variable.
            Object sqlParamObj = null;
            switch (queryType) {
                case QueryDataTypes.DATA_INTEGER: {
                        sqlParamObj = g_shuffledArrayInt64[workerId, i];
                        break;
                    }

                case QueryDataTypes.DATA_STRING: {
                        sqlParamObj = g_shuffledArrayString[workerId, i];
                        break;
                    }

                case QueryDataTypes.DATA_DATETIME: {
                        sqlParamObj = g_shuffledArrayDateTime[workerId, i];
                        break;
                    }

                case QueryDataTypes.DATA_STRING_LIKE: {
                        sqlParamObj = g_shuffledArrayStringLike[workerId, i];
                        break;
                    }

                case QueryDataTypes.DATA_STRING_STARTS_WITH: {
                        sqlParamObj = g_shuffledArrayString[workerId, i];
                        break;
                    }

                case QueryDataTypes.DATA_DECIMAL: {
                        sqlParamObj = g_shuffledArrayDecimal[workerId, i];
                        break;
                    }
            }

            // Fetching the enumerator.
            using (SqlEnumerator<Object> sqlEnum = (SqlEnumerator<Object>)Db.SQL(QueryStrings[(Int32)queryType], sqlParamObj).GetEnumerator()) {
                // Fetching only the first result and getting the object checksum.
                if (sqlEnum.MoveNext()) {
                    // Getting current result object.
                    TestClass curObj = (TestClass)sqlEnum.Current;

                    // Calculating object's checksum.
                    calcChecksum += curObj.GetCheckSum();

                    // Checking if there are more things in transaction.
                    if (performUpdate) {
                        // Updating the object.
                        curObj.DoSimpleUpdate(g_shuffledArrayString, TotalNumOfObjectsInDB, queryType, workerId);
                    }
                }
            }

            // Calculating artificially correct checksum and checking if both are the same.
            artChecksum += g_shuffledArrayInt64[workerId, i];

            // Checking for correct checksums.
            if (calcChecksum != artChecksum) {
                String errMessage = "Inconsistent checksums: [" + artChecksum + ", " + calcChecksum + "].";

                // Throwing an exception that will cause test failure.
                Console.WriteLine(errMessage);
                throw new Exception(errMessage);
            }
        }

        /// <summary>
        /// Does the commit certain amount of times.
        /// </summary>
        void CommitWithRetries(Transaction trans, Int32 retries)
        {
            for (Int32 r = 0; r < retries; r++)
            {
                try
                {
                    trans.Commit();
                }
                // Checking unhandled transaction conflict exception.
                catch (UnhandledTransactionConflictException)
                {
                    continue;
                }
                // Checking transaction conflict exception.
                catch (TransactionConflictException)
                {
                    continue;
                }

                // Successful commit.
                return;
            }
        }

        /// <summary>
        /// Populates the database with unique objects.
        /// </summary>
        void PopulateDatabase()
        {
            // Used to create unique objects.
            Int64 curObjectNum = 0;

            // Populating database using number of given transactions.
            var transactions = new List<Task>();
            for (Int32 t = 0; t < TransactionsMagnifier; t++)
            {

                var tran = Db.TransactAsync(() =>
                {

                    for (Int64 i = 0; i < NumObjectsPerTransaction; i++) {
                        TestClass testClassInstance = new TestClass(
                            true,
                            (Nullable<SByte>)(curObjectNum % 127),
                            (Nullable<Byte>)(curObjectNum % 255),
                            (Nullable<Int16>)(curObjectNum % 32767),
                            (Nullable<UInt16>)(curObjectNum % 65535),
                            (Nullable<Int32>)curObjectNum,
                            (Nullable<UInt32>)curObjectNum,
                            (Nullable<Int64>)curObjectNum,
                            (Nullable<UInt64>)curObjectNum,
                            (Nullable<Decimal>)curObjectNum,
                            (Nullable<Double>)curObjectNum,
                            (Nullable<Single>)curObjectNum,
                            new DateTime(curObjectNum),
                            new Binary(BitConverter.GetBytes(curObjectNum)),
                            curObjectNum.ToString()
                            );

                        // Creating next unique object.
                        curObjectNum++;
                    }
                });

                transactions.Add(tran);
            }

            Task.WaitAll(transactions.ToArray());
        }
    }
}