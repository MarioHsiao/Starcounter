using Starcounter.Apps.Package;
using System;
using System.IO;

namespace StarPack {
    class ArchivePack {

        public static void Execute(PackOptions options) {

            Archive archive = Archive.Create(options.File, options.Executable, options.ResourceFolder);

            options.Output = UpdateOutputPath(options.Output, archive);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\r\n Creating archive with:");
            Console.WriteLine("   Executable: {0}", archive.Executable);
            if (archive.ResourceFolder == null) {
                Program.PrintError("   Resource folder: *Warning* unset resource folder");
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else {
                Console.WriteLine("   Resource folder: {0}", archive.ResourceFolder);
            }
            Console.WriteLine("   Icon: {0}", archive.Icon ?? "N/A");
            Console.WriteLine();

            Console.WriteLine("  Manifest:");
            Console.WriteLine("   ID: {0}", archive.Config.ID ?? "N/A");
            Console.WriteLine("   Channel: {0}", archive.Config.Channel ?? "N/A");
            Console.WriteLine("   Version: {0}", archive.Config.Version ?? "N/A");
            Console.WriteLine();


            archive.Save(options.Output, true);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("succeeded -> {0}", options.Output);
            Console.ResetColor();
        }

        static string UpdateOutputPath(string output, Archive archive) {

            if (string.IsNullOrEmpty(output)) {
                output = Utils.PathAddBackslash(Directory.GetCurrentDirectory());
            }

            if (Utils.IsFolder(output) || Directory.Exists(output)) {
                string fileName = string.Format("{0}-{1}.zip", Path.GetFileNameWithoutExtension(archive.ProjectFile), archive.Config.Version);
                return Path.Combine(output, fileName);
            }

            // Add .zip extention
            if (!output.ToLower().EndsWith(".zip")) {
                output += ".zip";
            }

            return output;
        }
    }
}
