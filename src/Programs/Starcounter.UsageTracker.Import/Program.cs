using System;
using Starcounter;

namespace Starcounter.Applications.UsageTrackerApp.Import {
    class Program {
        static void Main() {

            // 1. Start new tracker with the new database
            // star -d=tracker "c:\github\Level1\bin\Debug\programs\usagetracker\Starcounter.UsageTracker.exe" 8282 c:\UsageTracker

            string file = @"d:\tmp\export\stripped.json";
            file = @"d:\tmp\export\dbExport.json";


            try {
                ImportManager importManager = new ImportManager();
                importManager.Import(file);

            }
            catch (Exception e) {
                Console.WriteLine("ERROR: " + e.ToString());
            }
        }
    }
}