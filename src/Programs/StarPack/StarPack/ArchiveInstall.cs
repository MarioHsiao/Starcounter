using Starcounter.Apps.Package;
using System;

namespace StarPack {
    public class ArchiveInstall {

        public static void Execute(InstallOptions options) {

            // Set default values
            if( string.IsNullOrEmpty(options.Databasename)) {
                options.Databasename = "default";
            }

            if( string.IsNullOrEmpty(options.Host)) {
                options.Host = "127.0.0.1";
            }

            if( options.Port == 0) {
                options.Port = Utils.GetSystemHttpPort();
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\r\n Installing archive:");
            Console.WriteLine("   Archive: {0}", options.File);
            Console.WriteLine("   Server: {0}:{1}", options.Host, options.Port);
            Console.WriteLine("   Database: {0}", options.Databasename);
            Console.WriteLine();

            Archive.Install(options.Host, options.Port, options.Databasename, options.File);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("succeeded -> {0}", options.Databasename);
            Console.ResetColor();
        }
    }
}
