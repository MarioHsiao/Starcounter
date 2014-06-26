using Starcounter.CommandLine;
using System;

namespace Starcounter.CLI {
    /// <summary>
    /// Represents a command that execute in the scope of a CLI
    /// application and that communicates with a target admin
    /// server to accomplish its task.
    /// </summary>
    public abstract class CLIClientCommand {
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