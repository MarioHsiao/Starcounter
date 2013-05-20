using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using Starcounter.Internal;
using System;
using System.Diagnostics;
using System.IO;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides the CLI specifics used when an executable is
    /// being ran from the OS shell (e.g. by double-clicking the
    /// executable).
    /// </summary>
    public static class Shell {
        /// <summary>
        /// Boots the executable that caused the current process to
        /// launch in the Starcounter code host.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this method is executed inside the code host process,
        /// it will silently return.
        /// </para>
        /// <para>
        /// This is the call that will be weaved, during compile-time,
        /// into executables that want to support shell bootstrapping.
        /// </para>
        /// </remarks>
        [DebuggerNonUserCode]
        public static void BootInHost() {
            if (IsCodeHostProcess(Process.GetCurrentProcess())) 
                return;
            
            var args = Environment.GetCommandLineArgs();
            var workingDirectory = Environment.CurrentDirectory;

            DoBootInHostAndExit(args, workingDirectory);
        }

        /// <summary>
        /// Determines whether <paramref name="p"/> reference a process
        /// that is considered a Starcounter code host process.
        /// </summary>
        /// <param name="p">The process to evaluate</param>
        /// <returns><c>true</c> if the process is a code host; <c>false</c>
        /// otherwise.</returns>
        public static bool IsCodeHostProcess(Process p) {
            return p.ProcessName.Equals(StarcounterConstants.ProgramNames.ScCode);
        }

        /// <summary>
        /// Performs the actual booting of the executable once it has been
        /// determined that the current process is not a code host.
        /// </summary>
        /// <param name="args">The arguments from the environment.</param>
        /// <param name="workingDirectory">The current working directory.</param>
        /// <remarks>
        /// The choice to have this method public mainly has to do with testing
        /// and instrumentation. By allowing it to be accessed from any client
        /// code, we can write test doubles and pass in explicit arguments from
        /// a fake environment. The real entrypoint is <see cref="BootInHost"/>
        /// and it's that code that is weaved in production.
        /// </remarks>
        public static void DoBootInHostAndExit(string[] args, string workingDirectory) {
            // From MSDN:
            // "The first element in the array contains the file name of the executing program.
            // If the file name is not available, the first element is equal to String.Empty."
            // 
            // and
            //
            // "The program file name can, but is not required to, include path information."
            //
            // Make sure we are prepared for both scenarios.
            var exePath = args[0];
            if (exePath == string.Empty) {
                Exit(ReportAboutMissingExePath());
            }
            if (string.IsNullOrEmpty(Path.GetDirectoryName(exePath))) {
                // It is null if we only pass a root, i.e. "c:\" and empty if
                // args[0] provides a single file name only. This fits our
                // needs pretty good.
                exePath = Path.Combine(workingDirectory, exePath);
            }
            exePath = Path.GetFullPath(exePath);

            // Define the syntax, parse the command-line in accordance to it
            // and pass it along to the Exec.

            ApplicationArguments appArgs;
            var syntax = DefineCommandLineSyntax();
            if (!SharedCLI.TryParse(args, syntax, out appArgs))
                return;

            if (appArgs.ContainsFlag(SharedCLI.UnofficialOptions.Debug)) {
                Debugger.Launch();
            }

            ExeCLI.Exec(exePath, appArgs);
        }

        static IApplicationSyntax DefineCommandLineSyntax() {
            ApplicationSyntaxDefinition appSyntax;
            CommandSyntaxDefinition commandSyntax;

            appSyntax = new ApplicationSyntaxDefinition();
            appSyntax.ProgramDescription = "scshell";
            appSyntax.DefaultCommand = "exec";
            SharedCLI.DefineWellKnownOptions(appSyntax, true);

            commandSyntax = appSyntax.DefineCommand(
                "exec", 
                "Executes an application", 0, int.MaxValue);

            return appSyntax.CreateSyntax();
        }

        static int ReportAboutMissingExePath() {
            var error = "Unable to retreive the path to the executable.";
            var help = "Please try instead \"star.exe <my.exe> [parameters]\" where <my.exe> is the name of your executable.";
            ConsoleUtil.ToConsoleWithColor(error, ConsoleColor.Red);
            ConsoleUtil.ToConsoleWithColor(help, ConsoleColor.Gray);
            return (int)Error.SCERRUNSPECIFIED;
        }

        static void Exit(int exitCode) {
            Exit(exitCode, exitCode != 0);
        }

        static void Exit(int exitCode, bool verbose = true) {
            if (verbose) {
                var msg = Environment.NewLine + "Press [ENTER] to exit...";
                Console.WriteLine(msg);
                Console.ReadLine();
            }
            Environment.Exit(exitCode);
        }
    }
}
