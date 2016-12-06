
using Starcounter.Weaver;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.CLI.Weaver
{
    /// <summary>
    /// Host used by the <see cref="CLIToolingWeaver"/>.
    /// </summary>
    public class ConsoleWriterWeaverHost : IWeaverHost
    {
        readonly List<uint> errors = new List<uint>();
        readonly Verbosity verbosity;

        /// <summary>
        /// 
        /// </summary>
        public enum Verbosity
        {
            /// <summary>
            /// Nothing is written.
            /// </summary>
            Quiet = 0,

            /// <summary>
            /// Errors and warnings are written.
            /// </summary>
            Minimal = 10,
            
            /// <summary>
            /// Errors, warnings and other kind of information is written.
            /// </summary>
            Verbose = 20,

            /// <summary>
            /// Everything is written, including debug messages.
            /// </summary>
            Diagnostic = 30
        }

        /// <summary>
        /// Gets the exit code conducted based on messages reaching us
        /// from the weaver.
        /// </summary>
        public uint ExitCode { get; private set; }

        /// <summary>
        /// Initialize a new <see cref="ConsoleWriterWeaverHost"/>.
        /// </summary>
        public ConsoleWriterWeaverHost()
        {
            var v = ConsoleWriterWeaverHost.Verbosity.Minimal;
            if (SharedCLI.Verbosity == OutputLevel.Verbose)
            {
                v = ConsoleWriterWeaverHost.Verbosity.Diagnostic;
            }

            this.verbosity = v;
        }
        
        void IWeaverHost.OnWeaverSetup(WeaverSetup setup)
        {
            var host = (IWeaverHost)this;
            host.WriteDebug("IWeaverHost.OnWeaverSetup");
        }

        void IWeaverHost.OnWeaverStart()
        {
            var host = (IWeaverHost)this;
            host.WriteDebug("IWeaverHost.OnWeaverStart");
        }

        void IWeaverHost.OnWeaverDone(bool result)
        {
            var host = (IWeaverHost)this;
            host.WriteDebug("IWeaverHost.OnWeaverDone");

            if (result)
            {
                Trace.Assert(errors.Count == 0);
                ExitCode = 0;
            }
            else
            {
                var e = errors.Count == 1 ? errors[0] : 0;
                if (e == default(uint))
                {
                    e = Error.SCERRWEAVINGERROR;
                }

                ExitCode = e;
            }
        }

        void IWeaverHost.WriteDebug(string message, params object[] parameters)
        {
            if (verbosity > Verbosity.Verbose)
            {
                WriteToConsole(string.Format(message, parameters), ConsoleColor.DarkGray);
            }
        }

        void IWeaverHost.WriteInformation(string message, params object[] parameters)
        {
            if (verbosity >= Verbosity.Verbose)
            {
                WriteToConsole(string.Format(message, parameters), ConsoleColor.White);
            }
        }

        void IWeaverHost.WriteWarning(string message, params object[] parameters)
        {
            if (verbosity >= Verbosity.Minimal)
            {
                WriteToConsole(string.Format(message, parameters), ConsoleColor.Cyan);
            }
        }

        void IWeaverHost.WriteError(uint code, string message, params object[] parameters)
        {
            if (verbosity > Verbosity.Quiet)
            {
                WriteToConsole(string.Format(message, parameters), ConsoleColor.Red);
            }
        }

        void WriteToConsole(string message, ConsoleColor? color = null)
        {
            if (!color.HasValue)
            {
                Console.WriteLine(message);
            }
            else
            {
                ConsoleUtil.ToConsoleWithColor(message, color.Value);
            }
        }
    }
}
