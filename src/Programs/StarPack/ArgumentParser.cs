using System;
using System.IO;
using System.Net;
using System.Reflection;

namespace StarPack {
    public class ArgumentParser {

        public static bool ParseArguments(string[] args, out PackOptions packOptions, out InstallOptions installOptions) {

            packOptions = null;
            installOptions = null;

            if (args.Length < 1) {
                PrintHelp();
                return true;
            }

            // Get command
            string cmd = args[0];
            bool result = false; ;
            if (cmd == "-i" || cmd == "--install") {
                result = ParseInstallOptions(args, out installOptions);
            }
            else if (cmd == "-p" || cmd == "--pack") {
                result = ParsePackOptions(args, out packOptions);
            }
            else if (cmd == "-v" || cmd == "--version") {
                PrintVersion();
                return true;
            }
            else if (cmd == "-h" || cmd == "--help") {
                PrintHelp();
                return true;
            }
            else {
                throw new InvalidOperationException(string.Format("Unknown command: {0}", cmd));
            }

            if (!result) {
                throw new InvalidOperationException("Unknown command");
            }

            return true;
        }

        static bool ParsePackOptions(string[] args, out PackOptions packOptions) {

            packOptions = new PackOptions();

            if (args.Length >= 2) {
                // Get file
                packOptions.File = args[1];
            }

            string arg;
            for (int i = 2; i < args.Length; i++) {

                arg = args[i];

                // Output
                if (arg.ToLower().StartsWith("-o=") || arg.ToLower().StartsWith("--output=")) {

                    if (packOptions.Output != null) {
                        throw new InvalidOperationException("Output parameter already set");
                    }

                    packOptions.Output = GetValue(arg);
                    if (string.IsNullOrEmpty(packOptions.Output)) {
                        throw new InvalidOperationException("Invalid ouput parameter value");
                    }

                    packOptions.Output = Path.GetFullPath(packOptions.Output);
                    continue;
                }

                // Resource
                if (arg.ToLower().StartsWith("-r=") || arg.ToLower().StartsWith("--resource=")) {

                    if (packOptions.ResourceFolder != null) {
                        throw new InvalidOperationException("Resource parameter already set");
                    }

                    packOptions.ResourceFolder = GetValue(arg);
                    if (string.IsNullOrEmpty(packOptions.ResourceFolder)) {
                        throw new InvalidOperationException("Invalid resource parameter value");
                    }

                    packOptions.ResourceFolder = Path.GetFullPath(packOptions.ResourceFolder);
                    continue;
                }

                // Executable
                if (arg.ToLower().StartsWith("-e=") || arg.ToLower().StartsWith("--executable=")) {

                    if (packOptions.Executable != null) {
                        throw new InvalidOperationException("Executable parameter already set");
                    }

                    packOptions.Executable = GetValue(arg);
                    if (string.IsNullOrEmpty(packOptions.Executable)) {
                        throw new InvalidOperationException("Invalid executable parameter value");
                    }

                    packOptions.Executable = Path.GetFullPath(packOptions.Executable);
                    continue;
                }

                // Project configuration
                if (arg.ToLower().StartsWith("-c=") || arg.ToLower().StartsWith("--config=")) {

                    if (packOptions.Projectconfiguration != null) {
                        throw new InvalidOperationException("Project configuration parameter already set");
                    }

                    packOptions.Projectconfiguration = GetValue(arg);
                    if (string.IsNullOrEmpty(packOptions.Projectconfiguration)) {
                        throw new InvalidOperationException("Project configuration parameter value");
                    }

                    continue;
                }

                // Build project
                if (arg.ToLower().StartsWith("-b") || arg.ToLower().StartsWith("--build")) {

                    packOptions.Build = true;
                    continue;
                }

                throw new InvalidOperationException(string.Format("Unknown command option ({0})", arg));
            }

            return true;
        }

        static bool ParseInstallOptions(string[] args, out InstallOptions installOptions) {

            installOptions = new InstallOptions();

            if (args.Length < 2) {
                PrintUsage();
                throw new InvalidOperationException("Invalid parameters");
            }
            // Get file
            installOptions.File = args[1];

            string arg;
            for (int i = 2; i < args.Length; i++) {

                arg = args[i];

                // Server ip[:port]
                if (arg.ToLower().StartsWith("-s=") || arg.ToLower().StartsWith("--server=")) {

                    string endPoint = GetValue(arg);

                    string[] ep = endPoint.Split(':');
                    if (ep.Length < 2) {
                        installOptions.Host = arg;
                        continue;
                    }

                    installOptions.Host = ep[0];
                    int port;
                    if (!int.TryParse(ep[1], out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort) {
                        throw new InvalidOperationException("Invalid server port");
                    }

                    installOptions.Port = (ushort)port;
                    continue;
                }

                // Database
                if (arg.ToLower().StartsWith("-d=") || arg.ToLower().StartsWith("--database=")) {

                    if (installOptions.Databasename != null) {
                        throw new InvalidOperationException("Database name parameter already set");
                    }

                    installOptions.Databasename = GetValue(arg);
                    if (string.IsNullOrEmpty(installOptions.Databasename)) {
                        throw new InvalidOperationException("Invalid databasename parameter value");
                    }

                    continue;
                }

                // Overwrite/uninstall existing app
                if (arg.ToLower().StartsWith("-f") || arg.ToLower().StartsWith("--force")) {

                    installOptions.Overwrite = true;
                    continue;
                }

                // Upgrade existing app
                if (arg.ToLower().StartsWith("-u") || arg.ToLower().StartsWith("--upgrade")) {

                    installOptions.Upgrade = true;
                    continue;
                }

                throw new InvalidOperationException(string.Format("Unknown command option ({0})", arg));
            }
            return true;
        }

        static void PrintUsage() {

            Console.WriteLine("Usage: starpack <command> [<archive>|<project>]|<achive> [options]");
            Console.WriteLine();
        }

        static void PrintHelp() {

            PrintUsage();
            Console.WriteLine(" Commands:");
            Console.WriteLine("   -h, --help           Help");
            Console.WriteLine("   -v, --version        Print version");
            Console.WriteLine("   -p, --pack           Pack");
            Console.WriteLine("   -i, --install        Install (Experimental)");
            Console.WriteLine();
            Console.WriteLine(" Pack Options (--pack):");
            Console.WriteLine("   -o=, --output=       Archive file");
            Console.WriteLine("   -c=, --config=       Project configuration ('release', 'debug')");
            Console.WriteLine("   -r=, --resource=     Resource folder");
            Console.WriteLine("   -e=, --executable=   Executable");
            Console.WriteLine("   -b, --build          Build project");
            Console.WriteLine();
            Console.WriteLine(" Install Options (--install):");
            Console.WriteLine("   -s=, --server        Server, ip[:port]");
            Console.WriteLine("   -d=, --database      Database name");
            Console.WriteLine("   -f=, --force         Overwrite exist app");
            Console.WriteLine("   -u=, --upgrade       Upgrade app");
            Console.WriteLine();
        }

        static string GetValue(string arg) {

            int i = arg.IndexOf('=');
            if (i == -1) return arg;

            return arg.Substring(i + 1);
        }

        static void PrintVersion() {
            Assembly assembly = typeof(Program).Assembly;
            string assemblyTitle = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute), false)).Title;
            Console.WriteLine("{0} version {1}", assemblyTitle, assembly.GetName().Version);
        }
        public static void PrintError(string txt) {

            Program.PrintError(txt);
        }
    }

    public class PackOptions {
        public string File;             // Fullpath to visual studio project file (.csproj)
        public string Output;           // Fullpath to output file (.zip)
        public string ResourceFolder;   // Full path to Resource folder
        public string Executable;       // Full path to Executable
        public string Projectconfiguration; // Visual studio project configuration
        public bool Build;              // Build Visual studio project 
    }
    public class InstallOptions {
        public string File;         // Fullapath to archive
        public string Host;         // Ip
        public ushort Port;            // Port, -1 = unset
        public string Databasename; // Dabasename
        public bool Overwrite;  // Overwrite/Uninstall existing app if it exists
        public bool Upgrade; // Upgrade existing app if it exists

    }
}
