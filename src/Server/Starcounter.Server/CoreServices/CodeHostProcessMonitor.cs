using Starcounter.Logging;
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Diagnostics;

namespace Starcounter.Server {

    internal sealed class CodeHostProcessMonitor {
        readonly LogSource log;

        public DatabaseEngineMonitor Monitor { get; set; }

        public string DatabaseName { get; set; }
        public int PID { get; set; }
        public DateTime StartTime { get; set; }

        public string WorkDescription {
            get {
                return string.Format("Synchronizing server state for exiting code host \"{0}\", PID={1}, StartTime={2}", DatabaseName, PID, StartTime);
            }
        }

        public bool Cancelled { get; private set; }

        public CodeHostProcessMonitor(DatabaseEngineMonitor monitor) {
            Monitor = monitor;
            log = monitor.Log;
        }

        public bool IsMonitorigDatabase(Database database) {
            return DatabaseName.Equals(database.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Cancel() {
            var wasCancelled = Cancelled == false;
            Cancelled = true;
            return wasCancelled;
        }

        public void CodeHostExited(object sender, EventArgs args) {
            var server = this.Monitor.Server;
            var cmd = new ActionCommand<Process>(this.Monitor.Server, Process, sender as Process, WorkDescription);
            server.CurrentPublicModel.Execute(cmd);
        }

        public void Process(ICommandProcessor processor, Process process) {
            try {
                ProcessStateSynchronization(processor, process);
            } finally {
                Monitor.RemoveCodeHostMonitor(this);
            }
        }

        void ProcessStateSynchronization(ICommandProcessor processor, Process process) {
            var server = this.Monitor.Server;

            if (Cancelled) {
                log.Debug("Ignoring synchronization of server state for {0}; monitoring was cancelled.");
                return;
            }

            Database database;
            var databaseExist = server.Databases.TryGetValue(DatabaseName, out database);
            if (!databaseExist) {
                // Might have been deleted.
                // Take no action.
                return;
            }

            var boundProcess = Monitor.GetCodeHostProcess(database);
            if (boundProcess != null) {
                // The database is bound to some other process. We should
                // let it be.
                return;
            }

            // Should we also remove from monitor.currentHosts?
            // TODO:

            Monitor.ResetInternalAndPublicState(server.DatabaseEngine, database, process);
        }
    }
}
