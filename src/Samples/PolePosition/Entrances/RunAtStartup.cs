using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.LucentObjects;
using System.IO;
using Starcounter.Poleposition;
using Starcounter.Poleposition.Entrances;
using Starcounter.TestFramework;

namespace PolepositionStartup
{
    public class RunAtStartup
    {
        // Name of the output file.
        const String OutputFileName = "PolePositionOutput.txt";

        /// <summary>
        /// Processes command line arguments.
        /// </summary>
        static void ProcessArgsParams(String[] args)
        {
            // Checking if database name is supplied as a parameter.
            if (args.Length > 0)
            {
                // Checking for help argument.
                if (String.Compare(args[0], "?") == 0)
                {
                    Console.WriteLine("PolePositionClient.exe [pause]");
                    Console.WriteLine("Pause: waits for key press to continue execution.");
                    Environment.Exit(1);
                }

                // Checking if application should pause.
                if (String.Compare(args[0], "pause", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                }
            }
        }

        /// <summary>
        /// Client console application entry point.
        /// </summary>
        static Int32 Main(string[] args)
        {
            // Processing command line arguments.
            ProcessArgsParams(args);

            // Creating indexes.
            CreateIndexes();

            // Since Main is started then we are on client.
            PolePositionEntrance pp = new PolePositionEntrance(false);

            // Starting the test.
            pp.RunLaps(null);

            return 0;
        }

        static bool CreateIndexes()
        {
            Db.SlowSQL("CREATE INDEX TestClassIndex1 ON LoadAndLatency.TestClass (prop_int64)");
            Db.SlowSQL("CREATE INDEX TestClassIndex2 ON LoadAndLatency.TestClass (prop_string)");
            Db.SlowSQL("CREATE INDEX TestClassIndex3 ON LoadAndLatency.TestClass (prop_datetime)");
            Db.SlowSQL("CREATE INDEX TestClassIndex4 ON LoadAndLatency.TestClass (prop_decimal)");

            Db.SlowSQL("CREATE INDEX TestClassIndex5 ON LoadAndLatency.TestClass (prop_int64_update)");
            Db.SlowSQL("CREATE INDEX TestClassIndex6 ON LoadAndLatency.TestClass (prop_string_update)");
            Db.SlowSQL("CREATE INDEX TestClassIndex7 ON LoadAndLatency.TestClass (prop_decimal_update)");
            Db.SlowSQL("CREATE INDEX TestClassIndex8 ON LoadAndLatency.TestClass (prop_int64_cycler)");

            Db.SlowSQL("CREATE INDEX SimpleObjectIndex1 ON LoadAndLatency.SimpleObject (fetchInt)");
            Db.SlowSQL("CREATE INDEX SimpleObjectIndex2 ON LoadAndLatency.SimpleObject (updateInt)");
            Db.SlowSQL("CREATE INDEX SimpleObjectIndex3 ON LoadAndLatency.SimpleObject (updateString)");

            return true;
        }
    }
}