
using Starcounter.Advanced;
using Starcounter.CommandLine;
using Starcounter.Internal;
using Starcounter.Hosting;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.IO;
using System.Net.Sockets;

namespace Starcounter.CLI {
    using EngineReference = EngineCollection.EnginesElementJson;
    using ExecutableReference = Engine.ExecutablesJson.ExecutingElementJson;
    using Option = Starcounter.CLI.SharedCLI.Option;
    using UnofficialOption = Starcounter.CLI.SharedCLI.UnofficialOptions;
    using Severity = Sc.Tools.Logging.Severity;

    /// <summary>
    /// Represents an CLI command that target an application only by its
    /// logical name.
    /// </summary>
    public abstract class NamedApplicationCLICommand {
        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        internal string ApplicationName { get; private set; }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        protected NamedApplicationCLICommand(string name) {
            ApplicationName = name;
        }
    }

    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start or stop an application.
    /// </summary>
    public abstract class ApplicationCLICommand : NamedApplicationCLICommand {
        readonly internal ApplicationBase Application;
        internal ApplicationArguments CLIArguments;
        internal string[] EntrypointArguments;
        internal AdminAPI AdminAPI;
        internal string ServerHost;
        internal int ServerPort;
        internal string ServerName;
        internal Node Node;
        internal StatusConsole Status;

        /// <summary>
        /// Gets the name of the database the current command
        /// target.
        /// </summary>
        public string DatabaseName { get; internal set; }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        protected ApplicationCLICommand(ApplicationBase app) : base(app.Name) {
            Application = app;
        }

        /// <summary>
        /// Executes the logic of the given CLI arguments on the
        /// application bound to the current instance, on the target
        /// database on the target server.
        /// </summary>
        public void Execute() {
            Node = new Node(ServerHost, (ushort)ServerPort);
            Status = StatusConsole.Open();
            var start = DateTime.Now;

            try {
                Run();
            } finally {
                Node = null;
                Status = null;
            }

            if (SharedCLI.ShowLogs) {
                WriteLogsToConsole(start);
            }
        }

        /// <summary>
        /// Runs the current command.
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Allows derived classes to initilize just after the
        /// command has been created and it's base class properties
        /// has been resolved.
        /// </summary>
        protected virtual void Initialize() {
        }

        internal static void HandleUnexpectedResponse(Response response) {
            var red = ConsoleColor.Red;
            int exitCode = response.StatusCode;

            Console.WriteLine();
            // Try extracting an error detail from the body, but make
            // sure that if we fail doing so, we just dump out the full
            // content in it's rawest format (dictated by the
            // Response.ToString implementation).
            try {
                var detail = new ErrorDetail();
                detail.PopulateFromJson(response.Body);
                //ConsoleUtil.ToConsoleWithColor(string.Format("  Starcounter error code: {0}", detail.ServerCode), red);
                ConsoleUtil.ToConsoleWithColor(detail.Text, red);
                Console.WriteLine();
                SharedCLI.ShowHints((uint)detail.ServerCode);
                exitCode = (int)detail.ServerCode;
            } catch {
                ConsoleUtil.ToConsoleWithColor("Unexpected response from server - unable to continue.", red);
                ConsoleUtil.ToConsoleWithColor(string.Format("  Response status code: {0}", response.StatusCode), red);
                ConsoleUtil.ToConsoleWithColor("  Response:", red);
                ConsoleUtil.ToConsoleWithColor(response.ToString(), red);
            } finally {
                Environment.Exit(exitCode);
            }
        }

        internal static void ShowVerbose(string output, ConsoleColor color = ConsoleColor.Yellow) {
            SharedCLI.ShowVerbose(output, color);
        }

        internal static void ShowHeadline(string headline) {
            if (SharedCLI.Verbosity > OutputLevel.Minimal) {
                ConsoleUtil.ToConsoleWithColor(headline, ConsoleColor.DarkGray);
            }
        }

        internal void ShowStatus(string status, bool onlyIfVerbose = false) {
            var show = !onlyIfVerbose || SharedCLI.Verbose;
            if (show) {
                Status.WriteTask(status);
                if (SharedCLI.Verbosity > OutputLevel.Minimal) {
                    ConsoleUtil.ToConsoleWithColor(string.Format("  - {0}", status), ConsoleColor.DarkGray);
                }
            }
        }

        internal static void ShowSocketErrorAndSetExitCode(SocketException ex, Uri serverUri, string serverName) {

            // Map the socket level error code to a correspoding Starcounter
            // error code. Try to be as specific as possible.

            uint scErrorCode;
            switch (ex.SocketErrorCode) {
                case SocketError.ConnectionRefused:
                    scErrorCode = Error.SCERRSERVERNOTRUNNING;
                    break;
                default:
                    scErrorCode = Error.SCERRSERVERNOTAVAILABLE;
                    break;
            }

            try {
                var serverInfo = string.Format("\"{0}\" at {1}:{2}", serverName, serverUri.Host, serverUri.Port);
                var socketError = string.Format("{0}/{1}: {2}", ex.SocketErrorCode, ex.ErrorCode, ex.Message);

                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor(
                    ErrorCode.ToMessage(scErrorCode, string.Format("(Server: {0})", serverInfo)),
                    ConsoleColor.Red);
                Console.WriteLine();
                ConsoleUtil.ToConsoleWithColor(
                    string.Format("(Socket error: {0})", socketError), ConsoleColor.DarkGray);

            } finally {
                // If any unexpected problem when constructing the error information
                // or writing them to the console, at least always set the error code.
                Environment.ExitCode = (int)scErrorCode;
            }
        }

        static void WriteLogsToConsole(DateTime started) {
            var count = int.MaxValue;
            var types = SharedCLI.LogDisplaySeverity;

            try {
                var console = new LogConsole();
                var reader = new FilterableLogReader() {
                    Count = count,
                    TypeOfLogs = types,
                    Since = started
                };

                var title = string.Format("Logs (since {0})", started.TimeOfDay);
                Console.WriteLine();
                Console.WriteLine(title);
                Console.WriteLine("".PadRight(title.Length, '-'));

                reader.Fetch((log) => { console.Write(log); });

            } catch (Exception e) {
                ConsoleUtil.ToConsoleWithColor(string.Format("Failed getting logs: {0}", e.Message), ConsoleColor.Red);
            }
        }
    }
}
