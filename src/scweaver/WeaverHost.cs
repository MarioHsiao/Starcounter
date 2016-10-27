
using Starcounter.Internal;
using System;
using System.IO;

namespace Starcounter.Weaver
{
    internal class WeaverHost : IWeaverHost
    {
        public Verbosity OutputVerbosity { get; set; }
        public string ErrorParcelID = string.Empty;
        public int MaxErrors { get; set; }
        public int ErrorCount { get; set; }

        public bool ShouldCreateParceledErrors {
            get { return !string.IsNullOrEmpty(ErrorParcelID); }
        }

        public WeaverHost()
        {
            OutputVerbosity = Verbosity.Default;
            ErrorParcelID = string.Empty;
            MaxErrors = 0;
            ErrorCount = 0;
        }

        public void OnWeaverSetup(WeaverSetup setup)
        {
        }

        public void OnWeaverStart()
        {
        }

        public void OnWeaverDone(bool result)
        {
        }

        public void WriteDebug(string message, params object[] parameters)
        {
            if (OutputVerbosity < Verbosity.Diagnostic)
                return;

            WriteWithColor(Console.Out, ConsoleColor.DarkGray, message, parameters);
        }

        public void WriteError(uint errorCode, string message, params object[] parameters)
        {
            InternalWriteError(message, parameters);
            if (Environment.ExitCode == 0)
            {
                Environment.ExitCode = (int)errorCode;
            }

            if (++ErrorCount == MaxErrors)
            {
                Environment.Exit(Environment.ExitCode);
            }
        }

        public void WriteInformation(string message, params object[] parameters)
        {
            if (OutputVerbosity < Verbosity.Verbose)
                return;

            WriteWithColor(Console.Out, ConsoleColor.White, message, parameters);
        }

        public void WriteWarning(string message, params object[] parameters)
        {
            if (OutputVerbosity < Verbosity.Minimal)
                return;

            WriteWithColor(Console.Out, ConsoleColor.Yellow, message, parameters);
        }

        void InternalWriteError(string message, params object[] parameters)
        {
            if (OutputVerbosity < Verbosity.Minimal)
                return;

            if (!string.IsNullOrEmpty(ErrorParcelID))
            {
                WriteParcel(ErrorParcelID, Console.Error, message, parameters);
            }

            WriteWithColor(Console.Error, ConsoleColor.Red, message, parameters);
        }

        static void WriteParcel(
            string parcelID,
            TextWriter stream,
            string message,
            params object[] parameters)
        {
            message = ParcelledError.Format(parcelID, message);
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
        static void WriteWithColor(TextWriter stream, ConsoleColor color, string message, params object[] parameters)
        {
            Console.ForegroundColor = color;
            try
            {
                stream.WriteLine(message, parameters);
            }
            finally
            {
                Console.ResetColor();
            }
        }
    }
}
