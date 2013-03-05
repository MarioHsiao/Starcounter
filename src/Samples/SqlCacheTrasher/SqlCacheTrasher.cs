using Microsoft.CSharp;
using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.LucentObjects;
using System.IO;
using Starcounter.TestFramework;

namespace SqlCacheTrasher
{
    public class SqlCacheTrasherCore
    {
        /// <summary>
        /// Maximum number of unique queries.
        /// </summary>
        public const Int32 MaxUniqueQueries = 8190;

        // Public property with name of the test.
        public const String TestName = "SqlCacheTrasher";

        // Maximum objects per transaction.
        const Int32 MaxObjPerTrans = 1000;

        // Indicates if test is started on the client.
        Boolean startedOnClient = false;

        // Total number of unique queries.
        Int32 numQueries = MaxUniqueQueries;

        // Number of queries per worker.
        Int32 numQueriesPerWorker = MaxUniqueQueries;

        // Represents the number of workers to run in parallel on machine.
        Int32 numWorkers = 0;

        // Indicates if each worker run same or independent queries.
        Boolean independentQueries = false;

        // Represents the number of logical cores on machine.
        Int32 numLogProc = 0;

        // Indicates that all threads has finished their work.
        Int32 numThreadsFinishedUnsafe = 0;

        // Used for logging test messages.
        TestLogger logger = null;

        // All combinations of property string capitalization.
        String[] queryCapCombs = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SqlCacheTrasherCore(Boolean startedOnClient, Int32 numLogProc, Int32 numWorkers, Int32 numQueries)
        {
            this.startedOnClient = startedOnClient;
            this.numLogProc = numLogProc;
            this.numWorkers = numWorkers;
            this.numQueries = numQueries;
            this.numQueriesPerWorker = numQueries / numWorkers;

            logger = new TestLogger(TestName, startedOnClient);
        }

        /// <summary>
        /// Logs some important event both for client and server side execution.
        /// </summary>
        public void LogEvent(String eventString)
        {
            logger.Log(eventString);
        }

        /// <summary>
        /// Main entry point for the tests.
        /// </summary>
        public void EntryPoint(Object state)
        {
            //System.Diagnostics.Debugger.Break();

            // Checking if we need to skip the process.
            if ((!startedOnClient) && (TestLogger.SkipInProcessTests()))
            {
                // Creating file indicating finish of the work.
                logger.Log(TestName + " in-process test is skipped!", TestLogger.LogMsgType.MSG_SUCCESS);

                return;
            }

            LogEvent("Starting " + TestName + " test.");

            // Purge existing database objects.
            DeleteAllObjects();

            // Populate the database with unique objects.
            PopulateDatabase();

            // Creating needed string combinations.
            queryCapCombs = CreateStringCapCombinations("IntegerProperty", numQueries);
            for (Int32 i = 0; i < numQueries; i++)
                queryCapCombs[i] = "SELECT s FROM SimpleObject s WHERE s." + queryCapCombs[i] + " = ?";

            // Running dependent queries.
            independentQueries = false;

            // Starting multiple workers.
            StartMultipleWorkers(numWorkers);

            // Running independent queries.
            independentQueries = true;

            // Starting multiple workers.
            StartMultipleWorkers(numWorkers);

            // Indicating successful finish of the work.
            logger.Log(TestName + " test finished successfully!", TestLogger.LogMsgType.MSG_SUCCESS);
        }

        /// <summary>
        /// Runs parallel test with multiple workers.
        /// </summary>
        void StartMultipleWorkers(Int32 workers)
        {
            // Setting number of workers.
            numWorkers = workers;

            LogEvent("Running in parallel " + numWorkers + " workers...");

            // Reseting worker finishing indicator.
            numThreadsFinishedUnsafe = 0;

            // Checking if we are on server.
            if (!startedOnClient)
            {
                // Starting workers as several Starcounter jobs.
                for (Byte i = 0; i < numWorkers; i++)
                {
                    DbSession dbs = new DbSession();

                    // Constructing parameter.
                    Byte workerId = i;

                    // Starting workers as Starcounter jobs.
                    if (i < numLogProc)
                        dbs.RunAsync(() => TestWorker(workerId), i);
                    else
                        dbs.RunAsync(() => TestWorker(workerId));
                }
            }
            else
            {
                // Starting workers as several system threads.
                for (Byte i = 0; i < numWorkers; i++)
                    System.Threading.ThreadPool.QueueUserWorkItem(new WaitCallback(TestWorkerClient), i);
            }

            // Waiting for all threads to finish.
            while (numThreadsFinishedUnsafe < numWorkers)
                Thread.Sleep(1000);
        }

        void TestWorkerClient(Object workerId)
        {
            // Special handling for client and multiple threads. Wrapping the call
            // to the test in a new session so that each thread is spread out on
            // different schedulers. This is needed since a session is bound to a 
            // specific scheduler and if no session is specified a default one is
            // used that is always connected to the same scheduler.
            //Db.Current.CreateSession();
            TestWorker((Byte)workerId);
            //Db.Current.CloseSession();
        }

        /// <summary>
        /// Core SQL test worker procedure.
        /// </summary>
        void TestWorker(Byte workerId)
        {
            // Determining the scheduler number for this test.
            Int32 startIndex = 0, endIndex = numQueries, hitCount = 0;

            // Checking if we are running independent queries.
            if (independentQueries)
            {
                startIndex = workerId * numQueriesPerWorker;
                endIndex = startIndex + numQueriesPerWorker;
            }

            Console.WriteLine(String.Format("Worker {0} operates on queries from {1} to {2}.", workerId, startIndex, endIndex - 1));

            // Repeating same thing several times.
            for (Int32 n = 0; n < 10000; n++)
            {
                using (Transaction transaction = Transaction.NewCurrent())
                {
                    for (Int32 i = startIndex; i < endIndex; i++)
                    {
                        hitCount = 0;
                        foreach (SimpleObject s in Db.SQL(queryCapCombs[i], i))
                        {
                            hitCount++;

                            // Checking that correct object fetched.
                            if (s.IntegerProperty != i)
                                throw new ArgumentOutOfRangeException("Wrong object fetched: " + s.IntegerProperty);
                        }

                        // Checking that exactly one object fetched.
                        if (hitCount != 1)
                            throw new ArgumentOutOfRangeException("Wrong hit count: " + hitCount);
                    }
                }
            }

            // Indicating that this worker has finished.
            Interlocked.Increment(ref numThreadsFinishedUnsafe);
        }

        /// <summary>
        /// Checks if objects already exist in database and deletes them all.
        /// </summary>
        void DeleteAllObjects()
        {
//            Int32 trans = 0;
            Int32 deleted = 0;

            using (Transaction transaction = Transaction.NewCurrent())
            {
                using (var sqlResult = Db.SQL("SELECT s FROM SimpleObject s").GetEnumerator())
                {
                    while (sqlResult.MoveNext())
                    {
                        // Deleting the object.
                        sqlResult.Current.Delete();
                        deleted++;

                        // Checking if we need to commit transaction.
                        /*trans++;
                        if (trans >= MaxObjPerTrans)
                        {
                            trans = 0;
                            transaction.Commit();

                            // Resetting the enumerator.
                            sqlResult.Reset();
                        }*/
                    }
                }

                // Committing transaction.
                transaction.Commit();
            }

            Console.Error.WriteLine("Deleted old objects: " + deleted);
        }

        /// <summary>
        /// Populates the database with unique objects.
        /// </summary>
        void PopulateDatabase()
        {
            Int32 trans = 0, created = 0;

            // Populating database using number of given transactions.
            using (Transaction transaction = Transaction.NewCurrent())
            {
                for (Int32 i = 0; i < numQueries; i++)
                {
                    SimpleObject testClassInstance = new SimpleObject(i);
                    created++;

                    // Checking if we need to commit transaction.
                    trans++;
                    if (trans >= MaxObjPerTrans)
                    {
                        trans = 0;
                        transaction.Commit();
                    }
                }

                // Committing all objects within transaction.
                transaction.Commit();
            }

            Console.Error.WriteLine("Created new objects: " + created);
        }

        /// <summary>
        /// Creates needed amount of combinations of letter capitalizations for the given string.
        /// </summary>
        static String[] CreateStringCapCombinations(String input, Int32 numCombs)
        {
            Char[] letters = input.ToLower().ToCharArray();

            // Checking the string capacity.
            if (numCombs > (2 << (letters.Length - 1)))
                throw new ArgumentOutOfRangeException("Inconsistent length of the input string and total number of combinations.");

            String[] combs = new String[numCombs];

            for (Int32 i = 0; i < numCombs; i++)
            {
                for (Int32 c = 0; c < letters.Length; c++)
                {
                    // Checking if we have an upper or lower case letter.
                    if (((i >> c) & 1) == 0)
                        letters[c] = Char.ToLower(letters[c]);
                    else
                        letters[c] = Char.ToUpper(letters[c]);
                }

                // Constructing final string.
                combs[i] = new String(letters);
            }

            return combs;
        }
    }
}