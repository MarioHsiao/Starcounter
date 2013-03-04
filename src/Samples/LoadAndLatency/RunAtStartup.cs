using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.IO;
using LoadAndLatency;
using Starcounter.TestFramework;

namespace LoadAndLatencyStartup
{
    public class RunAtStartup
    {
        /// <summary>
        /// Processes command line arguments.
        /// </summary>
        static void ProcessParams(String[] args, ref Int32 numWorkers, ref Int32 numTransactions, ref Boolean parallelTestOnly)
        {
            if (args.Length > 0)
            {
                // Checking for help argument.
                if (String.Compare(args[0], "?") == 0)
                {
                    Console.WriteLine("LoadAndLatencyClient.exe [NumberOfWorkers NumberOfTransactions [ParallelTestOnly] [pause]]");
                    Console.WriteLine("NumberOfWorkers: [1; 32]");
                    Console.WriteLine("NumberOfTransactions: [10; 10000]");
                    Console.WriteLine("ParallelTestOnly: s");
                    Console.WriteLine("Pause: waits for key press to continue execution.");
                    Environment.Exit(1);
                }

                // Getting number of workers.
                numWorkers = Int32.Parse(args[0]);

                // Fetching number of transactions.
                numTransactions = Int32.Parse(args[1]);

                // Checking if only parallel test.
                if (args.Length > 2)
                {
                    if (args[2] == "s")
                        parallelTestOnly = true;
                }

                // Checking if test should pause.
                if (String.Compare(args[args.Length - 1], "pause", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                }
            }
        }

        // Creating needed indexes for the test.
        static void CreateIndexes()
        {
            Db.SlowSQL("CREATE INDEX TestClassIndex1 ON LoadAndLatency.TestClass (prop_int64 ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex2 ON LoadAndLatency.TestClass (prop_string ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex3 ON LoadAndLatency.TestClass (prop_datetime ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex4 ON LoadAndLatency.TestClass (prop_decimal ASC)");

            Db.SlowSQL("CREATE INDEX TestClassIndex5 ON LoadAndLatency.TestClass (prop_int64_update ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex6 ON LoadAndLatency.TestClass (prop_string_update ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex7 ON LoadAndLatency.TestClass (prop_decimal_update ASC)");
            Db.SlowSQL("CREATE INDEX TestClassIndex8 ON LoadAndLatency.TestClass (prop_int64_cycler ASC)");

            Db.SlowSQL("CREATE INDEX SimpleObjectIndex1 ON LoadAndLatency.SimpleObject (fetchInt ASC)");
            Db.SlowSQL("CREATE INDEX SimpleObjectIndex2 ON LoadAndLatency.SimpleObject (updateInt ASC)");
            Db.SlowSQL("CREATE INDEX SimpleObjectIndex3 ON LoadAndLatency.SimpleObject (updateString ASC)");
        }

        /// <summary>
        /// Client console application entry point.
        /// </summary>
        static Int32 Main(string[] args)
        {
            Int32 numWorkers = 0, numTransactions = 0;
            Boolean parallelTestOnly = false;
            ProcessParams(args, ref numWorkers, ref numTransactions, ref parallelTestOnly);

            // Creating indexes.
            CreateIndexes();

            // Since Main is started then we are on client.
            LoadAndLatencyCore lal = new LoadAndLatencyCore(false, parallelTestOnly);

            // Modifying number of transactions.
            if (numTransactions > 0)
                lal.ChangeNumberOfTransactions(numTransactions);

            // Setting number of logical processors.
            lal.NumOfLogProc = Environment.ProcessorCount;

            // Checking if number of workers is supplied.
            if (numWorkers > 0)
            {
                lal.NumOfWorkers = numWorkers;
            }
            else
            {
                // Setting the default number of workers.
                lal.NumOfWorkers = lal.NumOfLogProc;
            }

            // Diagnostics.
            Console.WriteLine("Running LaL with " + lal.NumOfWorkers + " workers and " + lal.TransactionsNumber + " transactions multiplier.");

            // Starting the test.
            lal.EntryPoint(null);

            // Exiting test successfully.
            Environment.Exit(0);

            return 0;
        }
    }
}