using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;


//<PropertyGroup>
//    <!-- Force visual studio to use external compiler and avoid cached files -->
//    <UseHostCompilerIfAvailable>False</UseHostCompilerIfAvailable>
//  </PropertyGroup>




namespace SetVersionNumber
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("SetAssemblyVersion Tool v1.1");

            if (args == null || args.Length < 3)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("This tool modifies the <file> (AssemblyInfo.cs)");
                System.Console.WriteLine();

                Console.WriteLine("Usage: SetAssemblyVersion <command> <file> <version|serial> [</APPEND>]");
                Console.WriteLine("\tCommands:");
                Console.WriteLine("\t/A\tSet AssemblyVersion");
                Console.WriteLine("\t/F\tSet AssemblyFileVersion");
                Console.WriteLine("\t/I\tSet AssemblyInformationalVersion");
                Console.WriteLine("\t/S\tSet AssemblySerialInformation");
                Environment.Exit(1);
                return;
            }

            // Command
            string cmd = args[0];
            string searchText;
            if (string.Equals(cmd, "/A"))
            {
                searchText = "AssemblyVersion";
            }
            else if (string.Equals(cmd, "/F"))
            {
                searchText = "AssemblyFileVersion";
            }
            else if (string.Equals(cmd, "/I"))
            {
                searchText = "AssemblyInformationalVersion";
            }
            else if (string.Equals(cmd, "/S"))      // [assembly: AssemblySerialInformation("1234-1234-12234-1234-1234")]
            {
                searchText = "AssemblySerialInformation";
            }
            else
            {
                Console.WriteLine("Error: Invalid command (" + cmd + ")");
                Environment.Exit(2);
                return;
            }


            // File
            string assemblyfile = args[1];

            if (!File.Exists(assemblyfile))
            {
                Console.WriteLine("Error: File not found (" + assemblyfile + ")");
                Environment.Exit(2);
                return;
            }


            string information = string.Empty;

            if (string.Equals(cmd, "/S"))
            {
                // Serial
                try
                {
                    information = args[2];
                    System.Console.Write("Setting " + searchText + " to (" + information + ") in file " + assemblyfile + "...");
                }
                catch (Exception e)
                {
                    Console.WriteLine("*Error* Invalid serial format (" + e.Message + ")");
                    Environment.Exit(3);
                    return;
                }
            }
            else
            {
                // Version
                Version version;
                try
                {
                    version = new Version(args[2]);

                    information = version.ToString();
                    System.Console.Write("Setting " + searchText + " to (" + information + ") in file " + assemblyfile + "...");
                }
                catch (Exception e)
                {
                    Console.WriteLine("*Error* Invalid version format (" + e.Message + ")");
                    Environment.Exit(3);
                    return;
                }
            }

            bool bAppendIsMissing = false;
            if (args.Length == 4)
            {
                bAppendIsMissing = string.Equals("/APPEND", args[3]);
            }

            try
            {
                bool bFixed = SetAssemblyFileVersion(assemblyfile, "[assembly: " + searchText + "(", information, bAppendIsMissing);

                if (bFixed)
                {
                    Console.WriteLine("Done");
                }
                else
                {
                    Console.WriteLine("Skipped.");
                    Console.WriteLine("*Warning* " + searchText + " text entry not found.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Aborted.");
                Console.WriteLine("*Error*" + e.Message);
            }


        }

        /// <summary>
        /// Sets the assembly file version.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="searchString">The search string.</param>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        static bool SetAssemblyFileVersion(string file, string searchString, string information, bool bAppendIsMissing)
        {
            if (file == null) throw new ArgumentNullException("Invalid filename");
            if (string.IsNullOrEmpty(searchString)) throw new ArgumentNullException("Invalid SearchString");
            if (information == null) throw new ArgumentNullException("Invalid information");


            bool bFixed = false;
            string content = string.Empty;
            string line;
            StreamReader reader = new StreamReader(file);


            // TODO: Handle commented section like /* */ and // etc..

            while (true)
            {

                line = reader.ReadLine();
                if (line == null) break;

                if (bFixed == false && line.IndexOf(searchString, 0) == 0)
                {
                    content += searchString + "\"" + information + "\")]";
                    bFixed = true;
                }
                else
                {
                    content += line;
                }

                content += Environment.NewLine;

            }

            if (!bFixed && !bAppendIsMissing)
            {
                return bFixed;
            }

            if (bFixed == false && bAppendIsMissing)
            {
                // Append
                content += searchString + "\"" + information + "\")]";
            }


            reader.Close();
            reader.Dispose();

            //// Remove readonly attribute
            FileInfo info = new FileInfo(file);
            info.Attributes &= ~FileAttributes.ReadOnly;

            StreamWriter writer = new StreamWriter(file);
            writer.Write(content);
            writer.Close();
            writer.Dispose();

            if (bFixed == false && bAppendIsMissing)
            {
                // Append
                Console.Write("(Appended) ");
                return true;
            }


            return bFixed;
        }

    }
}
