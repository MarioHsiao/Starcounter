using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using Starcounter.LucentObjects;
using System.IO;
using SqlCacheTrasher;
using Starcounter.TestFramework;

namespace SqlCacheTrasherStartup
{
    public class RunAtStartup
    {
        /// <summary>
        /// Processes command line arguments.
        /// </summary>
        static void ProcessParams(String[] args, ref Int32 numQueries)
        {
            if (args.Length > 0)
            {
                // Checking for help argument.
                if (String.Compare(args[0], "?") == 0)
                {
                    Console.WriteLine(SqlCacheTrasherCore.TestName + " [NumberOfQueries] [Pause]");
                    Console.WriteLine("NumberOfQueries: [1; XXX]");
                    Console.WriteLine("Pause: waits for key press to continue execution.");
                    Environment.Exit(1);
                }

                // Getting number of queries per worker.
                numQueries = Int32.Parse(args[0]);

                // Checking if test should pause.
                if (String.Compare(args[args.Length - 1], "Pause", StringComparison.InvariantCultureIgnoreCase) == 0)
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
            // Calculating number of unique queries for the test.
            Int32 numQueries = SqlCacheTrasherCore.MaxUniqueQueries;
            ProcessParams(args, ref numQueries);
            if (numQueries > SqlCacheTrasherCore.MaxUniqueQueries)
            {
                Console.WriteLine("Maximum number of unique queries exceeded.");
                Environment.Exit(1);
            }

            // Creating indexes.
            CreateIndexes();

            // Since Main is started then we are on client.
            SqlCacheTrasherCore sct = new SqlCacheTrasherCore(
                false,
                Environment.ProcessorCount,
                Environment.ProcessorCount,
                numQueries);

            // Starting the test.
            sct.EntryPoint(null);

            // Exiting test successfully.
            Environment.Exit(0);

            return 0;
        }

        // Creating needed indexes for the test.
        static void CreateIndexes()
        {
            Db.SlowSQL("CREATE INDEX SimpleObjectIndex1 ON SqlCacheTrasher.SimpleObject (IntegerProperty ASC)");
        }
    }
}