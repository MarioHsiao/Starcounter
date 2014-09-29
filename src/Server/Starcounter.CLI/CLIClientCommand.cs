using Starcounter.CommandLine;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Net.Sockets;
using Sc.Tools.Logging;

namespace Starcounter.CLI {
    using LogEntry = Sc.Tools.Logging.LogEntry;

    /// <summary>
    /// Represents a command that execute in the scope of a CLI
    /// application and that communicates with a target admin
    /// server to accomplish its task.
    /// </summary>
    public abstract class CLIClientCommand {
        internal ApplicationArguments CLIArguments;
        internal AdminAPI AdminAPI;
        internal DateTime executionStartTime;

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
        /// Gets or sets a value that dictates of the command should
        /// write error logs to the console when <see cref="Run"/> has
        /// exited.
        /// </summary>
        public bool WriteErrorLogsToConsoleAfterRun { get; set; }

        /// <summary>
        /// Executes the logic of the given CLI arguments on the
        /// on the target database on the target server.
        /// </summary>
        public void Execute() {
            executionStartTime = DateTime.Now;
            Node = new Node(ServerHost, (ushort)ServerPort);
            Status = StatusConsole.Open();

            try {
                Run();
            } finally {
                Node = null;
                Status = null;
            }

            if (WriteErrorLogsToConsoleAfterRun) {
                CaptureAndWriteLoggedErrorsToConsole();
            }

            if (SharedCLI.ShowLogs) {
                WriteLogsToConsole(executionStartTime);
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

        internal void HandleUnexpectedResponse(Response response) {
            ErrorDetail detail = null;
            var red = ConsoleColor.Red;
            int exitCode = response.StatusCode;
            var showLogSummary = SharedCLI.ShowLogs;

            Console.WriteLine();

            var errorLogsWritten = CaptureAndWriteLoggedErrorsToConsole();

            // Try extracting an error detail from the body, but make
            // sure that if we fail doing so, we just dump out the full
            // content in it's rawest format (dictated by the
            // Response.ToString implementation).

            try {
                detail = new ErrorDetail();
                detail.PopulateFromJson(response.Body);
                exitCode = (int)detail.ServerCode;
            } catch {
                // Use the error code/message from the response as a final
                // fallback; done above.
            } finally {

                if (!errorLogsWritten || SharedCLI.Verbose) {
                    if (detail != null) {
                        ConsoleUtil.ToConsoleWithColor(detail.Text, red);
                        Console.WriteLine();
                        SharedCLI.ShowHints((uint)detail.ServerCode);
                    } else {
                        showLogSummary = true;
                        ConsoleUtil.ToConsoleWithColor("Unexpected response from server - unable to continue.", red);
                        ConsoleUtil.ToConsoleWithColor(string.Format("  Response status code: {0}", response.StatusCode), red);
                        ConsoleUtil.ToConsoleWithColor("  Response:", red);
                        ConsoleUtil.ToConsoleWithColor(response.ToString(), red);
                    }
                }

                if (showLogSummary) {
                    WriteLogsToConsole(executionStartTime);
                }

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

        bool CaptureAndWriteLoggedErrorsToConsole() {
            var log = new FilterableLogReader() {
                Count = int.MaxValue,
                Since = executionStartTime,
                TypeOfLogs = Severity.Error
            };
            var errors = LogSnapshot.Take(log, DatabaseName);
            var errorsToDisplay = errors.DatabaseLogs;
            if (errorsToDisplay.Length == 0) {
                errorsToDisplay = errors.All;
            }

            return WriteLoggedErrorsToConsole(errorsToDisplay) && errorsToDisplay.Length > 0;
        }

        static bool WriteLoggedErrorsToConsole(LogEntry[] entries) {
            try {
                var console = new LogConsole();
                foreach (var entry in entries) {
                    console.Write(entry);
                }
                
            } catch (Exception e) {
                ConsoleUtil.ToConsoleWithColor(string.Format("Failed getting logs: {0}", e.Message), ConsoleColor.Red);
                return false;
            }

            return true;
        }
    }
}