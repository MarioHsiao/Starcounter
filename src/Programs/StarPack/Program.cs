using Starcounter.Apps.Package;
using System;
using System.Reflection;

namespace StarPack {

    public class Program {
        static int Main(string[] args) {

            try {


//                Assembly assembly = Assembly.LoadFrom(@"c:\github\Level1\bin\Debug\Public Assemblies\Starcounter.dll");

                PackOptions packOptions;
                InstallOptions installOptions;

                ArgumentParser.ParseArguments(args, out packOptions, out installOptions);

                if (packOptions != null) {
                    ArchivePack.Execute(packOptions);
                }
                else if (installOptions != null) {
                    ArchiveInstall.Execute(installOptions);
                }
                else {
                    return (int)ExitCode.InvalidArguments;
                }
                return (int)ExitCode.NoError;
            }
            catch (InvalidOperationException e) {
                PrintError(e.Message);
                return (int)ExitCode.InvalidInput;
            }
            catch (InputErrorException e) {
                PrintError(e.Message);
                return (int)ExitCode.InvalidInput;
            }
            catch (Exception e) {
                PrintError(e.ToString());
                return (int)ExitCode.ProgramError;
            }
        }

        public static void PrintError(string txt) {

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(txt);
            Console.ResetColor();
        }

        public static void PrintWarning(string txt) {

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(txt);
            Console.ResetColor();
        }

        public enum ExitCode : int {
            NoError = 0,
            InvalidArguments = 1,
            InvalidInput = 2,
            ProgramError = 4
        }
    }
}
