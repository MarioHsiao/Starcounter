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
            ref Int32 minNightlyWorkers,
            ref Int32 transactionsMagnifier,
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
                else if (s.StartsWith("MinNightlyWorkers", StringComparison.InvariantCultureIgnoreCase))
                {
                    minNightlyWorkers = Int32.Parse(s.Substring(18));
                }
                else if (s.StartsWith("TransactionsMagnifier", StringComparison.InvariantCultureIgnoreCase))
                {
                    transactionsMagnifier = Int32.Parse(s.Substring(22));
                }
                else if (s.StartsWith("pause", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                }
                else if (s.StartsWith("?", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("LoadAndLatency.exe [SpecificTestType=0..2] [NumberOfWorkers=1..32] [MinNightlyWorkers=1..16] [TransactionsMagnifier=10..1000] [pause]");
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
            Int32 numWorkers = 0, transactionsMagnifier = 0, minNightlyWorkers = 0;
            LoadAndLatencyCore.LALSpecificTestType specificTestType = LoadAndLatencyCore.LALSpecificTestType.LAL_DEFAULT_TEST;

            // Processing command line arguments.
            if (!ProcessParams(
                args,
                ref numWorkers,
                ref minNightlyWorkers,
                ref transactionsMagnifier,
                ref specificTestType))
                return 0;

            // Creating indexes.
            CreateIndexes();

            // Since Main is started then we are on client.
            LoadAndLatencyCore lal = new LoadAndLatencyCore(false);

            // Modifying transactions magnifier.
            if (transactionsMagnifier > 0)
                lal.ChangeTransactionsMagnifier(transactionsMagnifier);

            // Setting specific type of test, if any.
            if (specificTestType != LoadAndLatencyCore.LALSpecificTestType.LAL_DEFAULT_TEST)
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

            // Checking if minimum nightly workers number is supplied.
            if (minNightlyWorkers > 0)
                lal.MinNightlyWorkers = minNightlyWorkers;

            // Starting the test.
            lal.EntryPoint();

            // Exiting test successfully.
            Environment.Exit(0);

            return 0;
        }
    }
}