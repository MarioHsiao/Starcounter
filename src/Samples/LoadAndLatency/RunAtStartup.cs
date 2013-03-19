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
        static bool ProcessParams(
            String[] args,
            ref Int32 numberOfWorkers,
            ref Int32 numberOfTransactions,
            ref LoadAndLatencyCore.LALSpecificTestType specificTestType)
        {
            foreach (String s in args)
            {
                if (s.StartsWith("SpecificTestType", StringComparison.InvariantCultureIgnoreCase))
                {
                    specificTestType = (LoadAndLatencyCore.LALSpecificTestType)Int32.Parse(s.Substring(17));
                }
                else if (s.StartsWith("NumberOfWorkers", StringComparison.InvariantCultureIgnoreCase))
                {
                    numberOfWorkers = Int32.Parse(s.Substring(16));
                }
                else if (s.StartsWith("NumberOfTransactions", StringComparison.InvariantCultureIgnoreCase))
                {
                    numberOfTransactions = Int32.Parse(s.Substring(21));
                }
                else if (s.StartsWith("pause", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                }
                else if (s.StartsWith("?", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("LoadAndLatency.exe [SpecificTestType=0..2] [NumberOfWorkers=1..32] [NumberOfTransactions=10..10000] [pause]");
                    return false;
                }
            }

            return true;
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
            LoadAndLatencyCore.LALSpecificTestType specificTestType = LoadAndLatencyCore.LALSpecificTestType.LAL_STANDARD_TEST;

            if (!ProcessParams(args, ref numWorkers, ref numTransactions, ref specificTestType))
                return 0;

            // Creating indexes.
            CreateIndexes();

            // Since Main is started then we are on client.
            LoadAndLatencyCore lal = new LoadAndLatencyCore(false);

            // Modifying number of transactions.
            if (numTransactions > 0)
                lal.ChangeNumberOfTransactions(numTransactions);

            // Setting specific type of test, if any.
            if (specificTestType != LoadAndLatencyCore.LALSpecificTestType.LAL_STANDARD_TEST)
                lal.SpecificTestType = specificTestType;

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
            lal.EntryPoint();

            // Exiting test successfully.
            Environment.Exit(0);

            return 0;
        }
    }
}