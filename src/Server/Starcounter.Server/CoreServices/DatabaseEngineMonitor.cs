
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
        /// Instructs this monitor to begin monitoring <paramref name="engineProc"/>
        /// part of the engine running <see cref="database"/>.
        /// </summary>
        /// <param name="database">The database the given engine process represent.</param>
        /// <param name="engineProc">The process to begin monitoring.</param>
        internal void BeginMonitoring(Database database, Process engineProc) {
            var description = string.Format("Synchronizing server state for exiting engine \"{0}\"", database.Name);

            if (engineProc.ProcessName.Equals(StarcounterConstants.ProgramNames.ScCode, StringComparison.InvariantCultureIgnoreCase)) {
                log.Debug("Begin monitoring code host process {0}, PID {1}, running database {2}", 
                    engineProc.ProcessName,
                    engineProc.Id,
                    database.Name);

                engineProc.EnableRaisingEvents = true;
                engineProc.Exited += (sender, args) => {
                    // Assure we check every exit in a thread-safe manner by
                    // posting to the queue and doing no evaulation what-so-ever
                    // here.
                    var x = new ActionCommand<Process, Database>(this.Server, ReactToCodeHostExit, engineProc, database, description);
                    this.Server.CurrentPublicModel.Execute(x);
                };
            }
        }

        void ReactToCodeHostExit(ICommandProcessor processor, Process processExited, Database database) {
            if (!database.SupposedToBeStarted) {
                // We don't do anything with databases that are safely
                // stopped (i.e. supposed not to run)
                return;
            }
            var engineService = Server.DatabaseEngine;

            // Try to get the process component currently associated with the
            // database. We expect it to be one, since it's supposed to be running.
            // If there isn't, we emit a warning and reset the database state to
            // what we expect it to be (not-running).
            int currentPID = GetProcessIdOrEmitWarning(database.CodeHostProcess, database);
            if (currentPID == -1) {
                ResetInternalAndPublicState(engineService, database, processExited);
                return;
            }

            // Try fetching the PID of the process component that has triggered
            // this command by exiting. Since we can't control to 100% the state
            // of this component, we just emit a warning if we can't access it
            // and then let the database state be in whatever state it is.
            int pid = GetProcessIdOrEmitWarning(processExited, database);
            if (pid == -1) {
                engineService.SafeClose(processExited);
                return;
            }

            log.Debug(
                "Updating state due to the exiting of code host process with PID {0}, running engine {1}", 
                pid, database.Name);

            // Check if the current state indicates the same process as the one
            // we have found exiting. If not, it's likely caused by a restart and
            // we should preserve the current state. This is a bit unusual, but
            // could happen in theory. Therefore, we log a notice about it.
            if (currentPID != pid) {
                log.LogNotice("Ignoring state update for code host with PID {0}; state already up-to-date.", pid);
                engineService.SafeClose(processExited);
                return;
            }

            // Update the state to reflect the unexpected exit of the
            // process. It's a bit unclear how we should treat the flag
            // SupposedToBeStarted - it could be advocated both that it
            // should be cleared and should be kept intact.

            ResetInternalAndPublicState(engineService, database, processExited);
        }

        void ResetInternalAndPublicState(DatabaseEngine engine, Database database, Process processExited) {
            engine.ResetToCodeHostNotRunning(database);
            engine.SafeClose(processExited);
            Server.CurrentPublicModel.UpdateDatabase(database);
        }

        int GetProcessIdOrEmitWarning(Process p, Database database) {
            string msg = null;
            if (p == null) {
                msg = "Process reference not assigned";
            } else {
                try {
                    int pid = p.Id;
                    return pid;
                } catch (Exception e) {
                    msg = e.Message;
                }
            }

            log.LogWarning("Unable to retreive PID of code host process for engine \"{0}\". Error: {1}.", database.Name, msg);
            return -1;
        }
    }
}