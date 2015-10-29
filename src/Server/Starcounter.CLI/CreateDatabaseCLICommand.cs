﻿
using Starcounter.CommandLine;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;

namespace Starcounter.CLI {

    /// <summary>
    /// Represents a CLI operation that creates a database.
    /// </summary>
    public sealed class CreateDatabaseCLICommand : CLIClientCommand {
        internal CreateDatabaseCLICommand() : base() { }

        /// <summary>
        /// Factory method used to create new commands.
        /// </summary>
        /// /// <param name="name">Optional explicit database name.</param>
        /// <param name="args">Optional well-known CLI-level arguments.</param>
        /// <returns>A new command</returns>
        public static CreateDatabaseCLICommand Create(string name = null, ApplicationArguments args = null) {
            args = args ?? ApplicationArguments.Empty;

            if (string.IsNullOrEmpty(name)) {
                SharedCLI.ResolveDatabase(args, out name);
            }

            var cmd = new CreateDatabaseCLICommand() {
                DatabaseName = name,
                AdminAPI = new AdminAPI(),
                CLIArguments = args,
            };
            
            cmd.ResolveServer(args);
            return cmd;
        }

        /// <inheritdoc/>
        protected override void Run() {
            if (this.Parent != null) {
                RunWithinContext();
                return;
            }

            ResponseExtensions.OnUnexpectedResponse = HandleUnexpectedResponse;

            try {
                Status.StartNewJob(string.Format("Creating database {0}", this.DatabaseName));
                ShowStatus("retrieving server status", true);
                if (!ServerServiceProcess.IsOnline()) {
                    SharedCLI.ShowErrorAndSetExitCode(ErrorCode.ToMessage(Error.SCERRSERVERNOTAVAILABLE), true);
                }

                RunWithinContext();
                ShowStartResultAndSetExitCode(this.Node);
            } catch (Exception e) {
                SharedCLI.ShowErrorAndSetExitCode(e, true, false);
                WriteErrorLogsToConsoleAfterRun = true;
            }
        }

        private void RunWithinContext() {
            var db = new Database();
            db.Name = DatabaseName;
            ShowStatus("creating database");
            var response = Node.POST(AdminAPI.Uris.Databases, db.ToJson(), null);
            response.FailIfNotSuccess();
        }

        void ShowStartResultAndSetExitCode(Node node) {
            Status.CompleteWithFinalJob("Created", string.Format("Name={0}", this.DatabaseName));
            Environment.ExitCode = 0;
        }
    }
}