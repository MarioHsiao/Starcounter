using System;
using Starcounter;
using System.IO;

namespace Starcounter.Applications.UsageTrackerApp.Export {
    class Program {
        static void Main(string[] args) {

            // 1. Start old tracker (exist in the App folder of this project) with the old database
            // star -d=tracker "c:\Users\Anders\Documents\Visual Studio 2012\Projects\ExportImport\Export\App\Starcounter.UsageTracker.exe" 8282 c:\UsageTracker

            try {
                Export.Start("D:\\tmp\\export\\dbExport.json");
            }
            catch (Exception e) {
                Console.WriteLine("ERROR: " + e.ToString());
            }

        }
    }
}