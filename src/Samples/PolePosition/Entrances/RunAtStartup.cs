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

            // Exiting test successfully.
            Environment.Exit(0);

            return 0;
        }

        // Creating indexes.
        static bool CreateIndexes()
        {
            Db.SlowSQL("CREATE INDEX BahrainPilot_Name ON Starcounter.Poleposition.Circuits.Bahrain.Pilot (Name ASC)");
            Db.SlowSQL("CREATE INDEX BahrainPilot_LicenseId ON Starcounter.Poleposition.Circuits.Bahrain.Pilot (LicenseId ASC)");
            Db.SlowSQL("CREATE INDEX Barcelona2_Field2 ON Starcounter.Poleposition.Circuits.Barcelona.Barcelona2 (Field2 ASC)");

            Db.SlowSQL("CREATE INDEX InheritIndexHack_00 ON Starcounter.Poleposition.Circuits.Barcelona.Barcelona4 (Field2 ASC)");
            Db.SlowSQL("CREATE INDEX ExtentScanHack_00 ON Starcounter.Poleposition.Circuits.Imola.Pilot (LicenseId ASC)");
            Db.SlowSQL("CREATE INDEX ExtentScanHack_01 ON Starcounter.Poleposition.Circuits.Melbourne.Pilot (LicenseId ASC)");
            Db.SlowSQL("CREATE INDEX ExtentScanHack_02 ON Starcounter.Poleposition.Circuits.Sepang.Tree (Depth ASC)");

            return true;
        }
    }
}