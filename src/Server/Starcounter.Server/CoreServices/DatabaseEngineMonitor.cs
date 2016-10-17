
using Starcounter.Internal;
using Starcounter.Logging;
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
        readonly HashSet<CodeHostProcessMonitor> hostMonitors;

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
            hostMonitors = new HashSet<CodeHostProcessMonitor>(new CodeHostProcessMonitor.EqualityComparer());
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
            log.Debug("Begin monitoring database hosting process {0}, PID {1}, running database {2}",
                engineProc.ProcessName,
                engineProc.Id,
                database.Name);

            var hostMonitor = new CodeHostProcessMonitor(this)
            {
                DatabaseName = database.Name,
                PID = engineProc.Id,
                StartTime = engineProc.StartTime
            };
            hostMonitors.Add(hostMonitor);
            currentHosts[database.Name] = hostMonitor;

            engineProc.EnableRaisingEvents = true;
            engineProc.Exited += hostMonitor.CodeHostExited;
        }

        internal void EndMonitoring(Database database) {
            var removed = currentHosts.Remove(database.Name);
            if (!removed) {
                // Log a notice; lets keep an eye on this
                log.LogNotice(string.Format("DatabaseEngineMonitor.EndMonitoring: Database {0} were not monitored", database.Name));
            }

            int cancelledMonitors = 0;
            foreach (var hostMonitor in hostMonitors) {
                if (hostMonitor.IsMonitoringDatabase(database)) {
                    if (hostMonitor.Cancel()) {
                        cancelledMonitors++;
                    }
                }
            }

            var msg = string.Format("Cancelled {0} monitor(s) for database {1}", cancelledMonitors, database.Name);
            if (cancelledMonitors == 0) {
                log.LogNotice(msg);
            } else {
                log.Debug(msg);
            }
        }

        internal void RemoveCodeHostMonitor(CodeHostProcessMonitor monitor) {
            CodeHostProcessMonitor current;
            if (currentHosts.TryGetValue(monitor.DatabaseName, out current)) {
                if (object.ReferenceEquals(current, monitor)) {
                    currentHosts.Remove(monitor.DatabaseName);
                }
            }
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