
using Starcounter.CommandLine;
using Starcounter.Rest.ExtensionMethods;
using Starcounter.Server;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Representations.JSON;
using System;
using System.Collections.Generic;
using System.Text;

namespace Starcounter.CLI {

    /// <summary>
    /// Represents a CLI operation that creates a database.
    /// </summary>
    public sealed class CreateDatabaseCLICommand : CLIClientCommand {
        Database databaseModel;

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
            cmd.databaseModel = new Database();
            
            return cmd;
        }

        /// <summary>
        /// Allow custom parmeters to be given when the database is to be created.
        /// </summary>
        /// <param name="parameters">Set of custom parameters</param>
        public void ParseAndApplyParameters(List<string> parameters) {
            var cfg = databaseModel.Configuration;

            foreach (var keyValueString in parameters) {
                var tokens = keyValueString.Split('=');
                if (tokens.Length != 2) {
                    throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, string.Format("Unable to parse parameter: {0}", keyValueString));
                }

                var key = tokens[0];
                var value = tokens[1];

                var template = cfg.Template.Properties[key];
                if (template == null) {
                    throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, string.Format("Parameter {0} is not accepted.", key));
                }

                var dataType = template.InstanceType;
                var supportedDataType = true;

                try {
                    if (dataType == typeof(string)) {
                        cfg[key] = value;
                    } else if (dataType == typeof(bool)) {
                        cfg[key] = bool.Parse(value);
                    } else if (dataType == typeof(decimal)) {
                        cfg[key] = decimal.Parse(value);
                    } else if (dataType == typeof(long)) {
                        cfg[key] = long.Parse(value);
                    } else if (dataType == typeof(double)) {
                        cfg[key] = double.Parse(value);
                    }
                    else {
                        supportedDataType = false;
                    }
                }
                catch (FormatException) {
                    throw ErrorCode.ToException(
                        Error.SCERRBADARGUMENTS, string.Format("Unable to parse value {0} of {1} as data type {2}.", value, key, template.JsonType));
                }

                if (!supportedDataType) {
                    throw ErrorCode.ToException(
                        Error.SCERRBADARGUMENTS, string.Format("Data type {0} of {1} is not supported as a parameter.", template.JsonType, key));
                }
            }
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
            var db = this.databaseModel;
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
