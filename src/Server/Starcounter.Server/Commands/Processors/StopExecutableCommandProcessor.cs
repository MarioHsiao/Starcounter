using Starcounter.Advanced;
using Starcounter.Bootstrap.Management;
using Starcounter.Internal;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Diagnostics;
using System.IO;
using Starcounter.Rest.ExtensionMethods;

namespace Starcounter.Server.Commands {

    /// <summary>
    /// Executes a queued and dispatched <see cref="StopExecutableCommand"/>.
    /// </summary>
    [CommandProcessor(typeof(StopExecutableCommand))]
    internal sealed partial class StopExecutableCommandProcessor : CommandProcessor {
        /// <summary>
        /// Initializes a new <see cref="StopExecutableCommandProcessor"/>.
        /// </summary>
        /// <param name="server">The server in which the processor executes.</param>
        /// <param name="command">The <see cref="StopExecutableCommand"/> the
        /// processor should exeucte.</param>
        public StopExecutableCommandProcessor(ServerEngine server, ServerCommand command)
            : base(server, command) {
        }

        /// <inheritdoc />
        protected override void Execute() {
            Database database;
            var command = this.Command as StopExecutableCommand;

            if (!this.Engine.Databases.TryGetValue(command.Name, out database)) {
                throw ErrorCode.ToException(Error.SCERRDATABASENOTFOUND, string.Format("Database: '{0}'.", command.DatabaseUri));
            }

            // Try finding the application among the set of applications we have
            // previously loaded into the given engine.

            var app = database.Apps.Find((candidate) => {
                return candidate.Key.Equals(command.Executable, StringComparison.InvariantCultureIgnoreCase);
            });
            if (app == null) {
                var shortName = command.Executable;
                foreach (var candidate in database.Apps) {
                    var candidateName = Path.GetFileName(candidate.OriginalExecutablePath);
                    if (candidateName.Equals(shortName, StringComparison.InvariantCultureIgnoreCase)) {
                        if (app == null) {
                            app = candidate;
                        } else {
                            // We have found a candidate previously who has got
                            // the same short name. They are ambigous and we can't
                            // use the short name to resolve.
                            app = null;
                            break;
                        }
                    }
                }
            }
            if (app == null) {
                throw ErrorCode.ToException(
                    Error.SCERREXECUTABLENOTRUNNING,
                    string.Format("Executable {0} is not running in database {1}.", command.Executable, database.Name)
                    );
            }

            Log.Debug("Stopping executable \"{0}\" in database \"{1}\"", app.OriginalExecutablePath, database.Name);

            // Clone the set of applications before we stop the host.
            // Remove the one we are currently stopping. Then stop the host
            // and restart every fellow applcation.

            var fellowApplications = database.Apps;
            fellowApplications.Remove(app);

            var stopped = Engine.DatabaseEngine.StopCodeHostProcess(database);
            if (!stopped) {
                throw ErrorCode.ToException(Error.SCERRDATABASEENGINENOTRUNNING, string.Format("Database {0}.", database.Name));
            }

            Process codeHost;
            Engine.DatabaseEngine.StartCodeHostProcess(database, out codeHost);
            Engine.DatabaseEngine.WaitUntilCodeHostOnline(codeHost, database);

            if (fellowApplications.Count > 0) {
                var node = Node.LocalhostSystemPortNode;
                var serviceUris = CodeHostAPI.CreateServiceURIs(database.Name);

                foreach (var fellow in fellowApplications) {
                    var exe = fellow.ToExecutable();

                    Log.Debug("Restarting executable \"{0}\" in database \"{1}\"", fellow.OriginalExecutablePath, database.Name);
                    if (exe.RunEntrypointAsynchronous) {
                        node.POST(serviceUris.Executables, exe.ToJson(), null, null, null, (Response resp, Object userObject) => { return null; });
                    } else {
                        var response = node.POST(serviceUris.Executables, exe.ToJson(), null, null);
                        response.FailIfNotSuccess();
                    }

                    database.Apps.Add(app);
                }
            }

            var result = Engine.CurrentPublicModel.UpdateDatabase(database);
            SetResult(result);
        }
    }
}
