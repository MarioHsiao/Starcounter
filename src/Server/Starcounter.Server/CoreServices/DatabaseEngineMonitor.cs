﻿
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Server {
    /// <summary>
    /// Governs the monitoring and reacting to database engine processes
    /// that exit.
    /// </summary>
    internal sealed class DatabaseEngineMonitor {
        readonly LogSource log = ServerLogSources.Processes;
        readonly Dictionary<string, CodeHostProcessMonitor> currentHosts;
        readonly List<CodeHostProcessMonitor> hostMonitors;

        /// <summary>
        /// Gets the server that has instantiated this monitor.
        /// </summary>
        internal readonly ServerEngine Server;

        /// <summary>
        /// Gets the log source used by this component when logging.
        /// </summary>
        internal LogSource Log {
            get { return log; }
        }

        /// <summary>
        /// Initializes a <see cref="DatabaseEngineMonitor"/> for the given
        /// <see cref="ServerEngine"/>.
        /// </summary>
        /// <param name="server">The server engine.</param>
        internal DatabaseEngineMonitor(ServerEngine server) {
            this.Server = server;
            currentHosts = new Dictionary<string, CodeHostProcessMonitor>();
            hostMonitors = new List<CodeHostProcessMonitor>();
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

                // Create a unique instance - the database name, the PID and the TICKS. And
                // specify it's instance method to be called when the process exit. And then
                // allow this monitor object to be reteived and CANCELLED (should be done
                // when we attempt to restart).
                // TODO:

                log.Debug("Begin monitoring code host process {0}, PID {1}, running database {2}",
                    engineProc.ProcessName,
                    engineProc.Id,
                    database.Name);

                var hostMonitor = new CodeHostProcessMonitor(this) {
                    DatabaseName = database.Name,
                    PID = engineProc.Id,
                    StartTime = engineProc.StartTime
                };
                hostMonitors.Add(hostMonitor);
                currentHosts[database.Name] = hostMonitor;

                engineProc.EnableRaisingEvents = true;
                engineProc.Exited += hostMonitor.CodeHostExited;
            }
        }

        internal void EndMonitoring(Database database) {
            var removed = currentHosts.Remove(database.Name);
            if (!removed) {
                // Log a notice; lets keep an eye on this
                log.LogNotice(string.Format("DatabaseEngineMonitor.EndMonitoring: Database {0} were not monitored", database.Name));
            }

            int cancelledMonitors = 0;
            foreach (var hostMonitor in hostMonitors) {
                if (hostMonitor.IsMonitorigDatabase(database)) {
                    if (hostMonitor.Cancel()) {
                        cancelledMonitors++;
                    }
                }
            }

            var msg = string.Format("Cancelled {0} monitors for database {1}", cancelledMonitors, database.Name);
            if (cancelledMonitors == 0) {
                log.LogNotice(msg);
            } else {
                log.Debug(msg);
            }
        }

        internal void RemoveCodeHostMonitor(CodeHostProcessMonitor monitor) {
            hostMonitors.Remove(monitor);
        }

        internal Process GetCodeHostProcess(Database database) {
            CodeHostProcessMonitor procRef;
            if (!currentHosts.TryGetValue(database.Name, out procRef)) {
                return null;
            }

            try {
                return Process.GetProcessById(procRef.PID);
            } catch (ArgumentException) {
            }

            return null;
        }

        internal void ResetInternalAndPublicState(DatabaseEngine engine, Database database, Process processExited) {
            engine.ResetToCodeHostNotRunning(database);
            Server.CurrentPublicModel.UpdateDatabase(database);
            if (processExited != null) {
                engine.SafeClose(processExited);
            }
        }
    }
}