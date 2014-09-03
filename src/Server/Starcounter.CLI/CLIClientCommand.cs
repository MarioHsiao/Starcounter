using Starcounter.CommandLine;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Net.Sockets;

namespace Starcounter.CLI {
    /// <summary>
    /// Represents a command that execute in the scope of a CLI
    /// application and that communicates with a target admin
    /// server to accomplish its task.
    /// </summary>
    public abstract class CLIClientCommand {
        internal ApplicationArguments CLIArguments;
        internal AdminAPI AdminAPI;

        /// <summary>
        /// Gets or sets the host of the admin server to
        /// target.
        /// </summary>
        public string ServerHost { get; set; }

        /// <summary>
        /// Gets or sets the port of the admin server to
        /// target.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// Gets or sets the logical server name of the
        /// admin server being targetted.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets the <see cref="Node"/> used by the current
        /// command when executed.
        /// </summary>
        protected Node Node { get; private set; }

        /// <summary>
        /// Gets the <see cref="StatusConsole"/> opened by
        /// the current command when executed.
        /// </summary>
        protected StatusConsole Status { get; private set; }

        /// <summary>
        /// Gets the name of the database the current command
        /// target, if applicable.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Executes the logic of the given CLI arguments on the
        /// on the target database on the target server.
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
        /// Resolves the server properties of the current instance
        /// using the given application arguments and/or defaults.
        /// </summary>
        /// <param name="args">Application arguments possibly containing
        /// specified well-known CLI admin server properties.</param>
        public void ResolveServer(ApplicationArguments args) {
            string host, name;
            int port;
            SharedCLI.ResolveAdminServer(args, out host, out port, out name);
            ServerHost = host;
            ServerPort = port;
            ServerName = name;
        }

        /// <summary>
        /// Writes a new status to the underlying <see cref="StatusConsole"/>.
        /// </summary>
        /// <param name="status">The new status message.</param>
        /// <param name="onlyIfVerbose">Tells the method to write only if
        /// the CLI context indicates a verbose mode.</param>
        protected void ShowStatus(string status, bool onlyIfVerbose = false) {
            var show = !onlyIfVerbose || SharedCLI.Verbose;
            if (show) {
                Status.WriteTask(status);
                if (SharedCLI.Verbosity > OutputLevel.Minimal) {
                    ConsoleUtil.ToConsoleWithColor(string.Format("  - {0}", status), ConsoleColor.DarkGray);
                }
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