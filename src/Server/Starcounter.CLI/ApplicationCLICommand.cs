﻿
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

    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start or stop an application.
    /// </summary>
    public abstract class ApplicationCLICommand {
        internal AdminAPI AdminAPI;
        internal string ServerHost;
        internal int ServerPort;
        internal string ServerName;
        internal string DatabaseName;
        internal ApplicationBase Application;
        internal ApplicationArguments CLIArguments;
        internal string[] EntrypointArguments;
        internal Node Node;
        internal StatusConsole Status;

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        protected ApplicationCLICommand() {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ApplicationCLICommand"/> class based on
        /// the given arguments. This instance can thereafter be executed with
        /// the <see cref="Execute"/> method.
        /// </summary>
        /// <param name="applicationFilePath">The application file.</param>
        /// <param name="exePath">The compiled application file.</param>
        /// <param name="args">Arguments given to the CLI host.</param>
        /// <param name="entrypointArgs">Arguments that are to be passed along
        /// to the application entrypoint, if the given arguments indicate it's
        /// being started/restarted.</param>
        /// <returns>An instance of <see cref="ApplicationCLICommand"/>.</returns>
        public static ApplicationCLICommand Create(
            string applicationFilePath,
            string exePath,
            ApplicationArguments args,
            string[] entrypointArgs = null) {
            if (string.IsNullOrWhiteSpace(applicationFilePath)) {
                applicationFilePath = exePath;
            }    
            
            ApplicationCLICommand command;
            if (args.ContainsFlag(Option.Stop)) {
                command = new StopApplicationCommand();
            } else {
                command = new StartApplicationCommand();
            }
            
            string appName;
            string workingDirectory;
            ResolveWorkingDirectory(args, out workingDirectory);
            SharedCLI.ResolveApplication(args, applicationFilePath, out appName);
            var app = new ApplicationBase(appName, applicationFilePath, exePath, workingDirectory, entrypointArgs);

            SharedCLI.ResolveAdminServer(args, out command.ServerHost, out command.ServerPort, out command.ServerName);
            SharedCLI.ResolveDatabase(args, out command.DatabaseName);

            command.Application = app;
            command.AdminAPI = new AdminAPI();
            command.CLIArguments = args;
            command.EntrypointArguments = entrypointArgs;
            
            return command;
        }

        /// <summary>
        /// Executes the logic of the given CLI arguments on the
        /// application bound to the current instance, on the target
        /// database on the target server.
        /// </summary>
        public void Execute() {
            Node = new Node(ServerHost, (ushort)ServerPort);
            Status = StatusConsole.Open();

            try {
                Run();
            } finally {
                Node = null;
                Status = null;
            }
        }

        /// <summary>
        /// Runs the current command.
        /// </summary>
        protected abstract void Run();

        static void ResolveWorkingDirectory(ApplicationArguments args, out string workingDirectory) {
            string dir;
            if (!args.TryGetProperty(Option.ResourceDirectory, out dir)) {
                dir = Environment.CurrentDirectory;
            }
            workingDirectory = dir;
            workingDirectory = Path.GetFullPath(workingDirectory);
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
    }
}
