
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Diagnostics;

namespace Starcounter.Server {
    /// <summary>
    /// Governs the monitoring and reacting to database engine processes
    /// that exit.
    /// </summary>
    internal sealed class DatabaseEngineMonitor {
        readonly LogSource log = ServerLogSources.Default;
        
        /// <summary>
        /// Gets the server that has instantiated this monitor.
        /// </summary>
        internal readonly ServerEngine Server;

        /// <summary>
        /// Initializes a <see cref="DatabaseEngineMonitor"/> for the given
        /// <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="server">The server engine.</param>
        internal DatabaseEngineMonitor(ServerEngine server) {
            this.Server = server;
        }

        /// <summary>
        /// Performs setup of the current <see cref="DatabaseEngineMonitor"/>.
        /// </summary>
        internal void Setup() {
        }

        /// <summary>
        /// Instructs this monitor to begin monitoring <paramref name="engineProcess"/>
        /// part of the engine running <see cref="database"/>.
        /// </summary>
        /// <param name="database">The database the given engine process represent.</param>
        /// <param name="engineProcess">The process to begin monitoring.</param>
        internal void BeginMonitoring(Database database, Process engineProcess) {
            var description = string.Format("Synchronizing server state for exiting engine \"{0}\"", database.Name);

            if (engineProcess.ProcessName.Equals(
                StarcounterConstants.ProgramNames.ScCode, StringComparison.InvariantCultureIgnoreCase)) {
                log.Debug("Begin monitoring code host process {0}, PID {1}, running database {2}", engineProcess.ProcessName, engineProcess.Id, database.Name);
                engineProcess.EnableRaisingEvents = true;
                engineProcess.Exited += (sender, args) => {
                    log.Debug("Detected exiting of code host with PID {0}, running database {1}", engineProcess.Id, database.Name);
                    var x = new ActionCommand<Process, Database>(this.Server, ReactToCodeHostExit, engineProcess, database, description);
                    this.Server.CurrentPublicModel.Execute(x);
                };
            }
        }

        void ReactToCodeHostExit(ICommandProcessor processor, Process processExited, Database database) {
            log.Debug("Updating state due to the exiting of code host process with PID {0}, running database {1}", 
                processExited.Id, database.Name);

            if (!database.SupposedToBeStarted || (database.CodeHostProcess != null && database.CodeHostProcess.Id != processExited.Id)) {
                log.Debug("Ignoring state update for code host with PID {0}; state already up-to-date.", processExited.Id);
                return;
            }

            // Update the state to reflect the unexpected exit of the
            // process. It's a bit unclear how we should treat the flag
            // SupposedToBeStarted - it could be advocated both that it
            // should be cleared and should be kept intact.

            database.Apps.Clear();
            database.CodeHostProcess = null;
            database.SupposedToBeStarted = false;
            processExited.Close();

            Server.CurrentPublicModel.UpdateDatabase(database);
        }
    }
}