
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using Starcounter.CommandLine;
using Starcounter.CommandLine.Syntax;
using System.IO;

namespace Weaver {

    class Program {

        static Verbosity OutputVerbosity = Verbosity.Default;
        static string ErrorParcelID = string.Empty;

        internal static bool IsCreatingParceledErrors {
            get { return !string.IsNullOrEmpty(Program.ErrorParcelID); }
        }

        static void Main(string[] args) {
            ApplicationArguments arguments;

            try {
                if (TryGetProgramArguments(args, out arguments))
                    ExecuteCommand(arguments);
            } finally {
                Console.ResetColor();
            }
        }

        static void ExecuteCommand(ApplicationArguments arguments) {
            // Implement.
            // TODO:
        }

        static bool TryGetProgramArguments(string[] args, out ApplicationArguments arguments) {
            arguments = null;
            return false;
        }

        static void Usage(
            IApplicationSyntax syntax,
            InvalidCommandLineException argumentException) {
            // Print a usage message, based on the syntax.
            // If the argument exception given is not null, print a header with
            // the message.

            if (argumentException != null) {
                Console.WriteLine(argumentException.Message);
                Console.WriteLine();
            } else {
                Console.WriteLine(syntax.ProgramDescription);
                Console.WriteLine();
            }

            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.Write("codelibrary.exe");
            foreach (var globalFlag in syntax.Flags) {
                Console.Write(" --FLAG:{0}", globalFlag.Name);
            }

            foreach (var globalProperty in syntax.Properties) {
                Console.Write(" --{0}=(value)", globalProperty.Name);
            }

            for (int i = 0; i < syntax.Commands.Length; i++) {
                var currentCommand = syntax.Commands[i];
                if (i > 0)
                    Console.Write(" or");
                Console.Write(" {0}", currentCommand.Name);
                if (currentCommand.MinParameterCount == 0 && currentCommand.MaxParameterCount == 0) {
                    // No parameters
                } else if (currentCommand.MinParameterCount == currentCommand.MaxParameterCount) {
                    Console.Write(" ({0} parameter(s))", currentCommand.MinParameterCount);
                } else {
                    Console.Write(" ({0}-{1} parameters)", currentCommand.MinParameterCount, currentCommand.MaxParameterCount);
                }

                foreach (var flag in currentCommand.Flags) {
                    Console.Write(" --FLAG:{0}", flag.Name);
                }

                foreach (var property in currentCommand.Properties) {
                    Console.Write(" --{0}=(value)", property.Name);
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Sets the environment exit code to the given Starcounter error
        /// code, after first writing its message to the console.
        /// </summary>
        /// <param name="errorCode">The code we should report as the exit
        /// code of the program.</param>
        /// <returns>True if the code was set as the exit code. False if
        /// a previous exit code was already set. In this case, only the
        /// error message is written to the console.</returns>
        internal static bool ReportProgramError(uint errorCode) {
            return ReportProgramError(errorCode, ErrorCode.ToMessage(errorCode));
        }

        /// <summary>
        /// Sets the environment exit code to the given Starcounter error
        /// code, after first writing the supplied message to the console.
        /// </summary>
        /// <param name="errorCode">The code we should report as the exit
        /// code of the program.</param>
        /// <param name="message">Error message that should be written to
        /// the console.</param>
        /// <param name="parameters">Possible message arguments.</param>
        /// <returns>True if the code was set as the exit code. False if
        /// a previous exit code was already set. In this case, only the
        /// error message is written to the console.</returns>
        internal static bool ReportProgramError(
            uint errorCode,
            string message,
            params object[] parameters) {
            WriteError(message, parameters);
            if (Environment.ExitCode == 0) {
                Environment.ExitCode = (int)errorCode;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes a debug message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteDebug(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Diagnostic)
                return;

            WriteWithColor(Console.Out, ConsoleColor.DarkGray, message, parameters);
        }

        /// <summary>
        /// Writes an information message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteInformation(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Verbose)
                return;

            WriteWithColor(Console.Out, ConsoleColor.White, message, parameters);
        }

        /// <summary>
        /// Writes a warning message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteWarning(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Minimal)
                return;

            WriteWithColor(Console.Out, ConsoleColor.Yellow, message, parameters);
        }

        /// <summary>
        /// Writes a error message to the console, if the verbosity level of
        /// the program is letting it through.
        /// </summary>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        internal static void WriteError(string message, params object[] parameters) {
            if (Program.OutputVerbosity < Verbosity.Minimal)
                return;

            if (!string.IsNullOrEmpty(Program.ErrorParcelID)) {
                WriteParcel(Program.ErrorParcelID, Console.Error, message, parameters);
            }

            WriteWithColor(Console.Error, ConsoleColor.Red, message, parameters);
        }

        private static void WriteParcel(
            string parcelID,
            TextWriter stream,
            string message,
            params object[] parameters) {
            message = string.Format("{0}{1}{2}", parcelID, message, parcelID);
            WriteWithColor(
                stream,
                ConsoleColor.DarkBlue,
                message,
                parameters
                );
        }

        /// <summary>
        /// Writes a message to the console, to the specific stream, using
        /// a specified color.
        /// </summary>
        /// <param name="stream">The console stream to write to.</param>
        /// <param name="color">The color to use.</param>
        /// <param name="message">Message to write.</param>
        /// <param name="parameters">Possible message arguments.</param>
        private static void WriteWithColor(TextWriter stream, ConsoleColor color, string message, params object[] parameters) {
            Console.ForegroundColor = color;
            try {
                stream.WriteLine(message, parameters);
            } finally {
                Console.ResetColor();
            }
        }
    }
}
