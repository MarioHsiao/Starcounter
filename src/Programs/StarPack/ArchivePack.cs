using Starcounter.Apps.Package;
using System;
using System.IO;

namespace StarPack {
    class ArchivePack {

        public static void Execute(PackOptions options) {

            int warnings = 0;
            Archive archive = Archive.Create(options.File, options.Build, options.Projectconfiguration, options.Executable, options.ResourceFolder);

            options.Output = UpdateOutputPath(options.Output, archive);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("Creating archive:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" Executable: {0}", archive.Executable);

            if (archive.ResourceFolder == null) {
                warnings++;
                Program.PrintWarning(" Resource folder: unset resource folder");
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }
            else {
                Console.WriteLine(" Resource folder: {0}", archive.ResourceFolder);
            }

            if (string.IsNullOrEmpty(archive.Icon)) {
                warnings++;
                Program.PrintWarning(" Icon: N/A");
            }
            else {
                Console.WriteLine(" Icon: {0}", archive.Icon ?? "N/A");

            }
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Manifest:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" ID: {0}", archive.Config.ID ?? "N/A");
            Console.WriteLine(" Name: {0}", archive.Config.DisplayName ?? "N/A");
            //Console.WriteLine(" Channel: {0}", archive.Config.Channel ?? "N/A");
            Console.WriteLine(" Version: {0}", archive.Config.Version ?? "N/A");
            Console.WriteLine(" VersionDate: {0} (local)", archive.Config.VersionDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            Console.WriteLine();

            if (archive.AssemblyInfoDate != DateTime.MinValue && archive.AssemblyInfoDate > archive.Config.VersionDate) {
                warnings++;
                Program.PrintWarning(" AssemblyInfo.cs has been modified, executable outofdate.");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }


            archive.Save(options.Output, true);

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(" Package -> {0}", options.Output);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("Package succeeded, {0} warnings.", warnings);
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
