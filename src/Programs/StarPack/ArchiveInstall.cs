using Starcounter.Apps.Package;
using System;

namespace StarPack {
    public class ArchiveInstall {

        public static void Execute(InstallOptions options) {

            // Set default values
            if (string.IsNullOrEmpty(options.Databasename)) {
                options.Databasename = "default";
            }

            if (string.IsNullOrEmpty(options.Host)) {
                options.Host = "127.0.0.1";
            }

            if (options.Port == 0) {
                options.Port = Utils.GetSystemHttpPort();
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("Installing archive:");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" Archive: {0}", options.File);
            Console.WriteLine(" Server: {0}:{1}", options.Host, options.Port);
            Console.WriteLine(" Database: {0}", options.Databasename);
            Console.WriteLine();

            Archive.Install(options.Host, options.Port, options.Databasename, options.File);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" {0} -> {1}", System.IO.Path.GetFileName(options.File), options.Databasename);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine();
            Console.WriteLine("Installation done (result unknown)");
            Console.ResetColor();
        }
    }
}
