using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Starcounter.Server {

    internal sealed class CodeHostProcessMonitor {
        readonly LogSource log;

        public sealed class EqualityComparer : IEqualityComparer<CodeHostProcessMonitor> {

            public bool Equals(CodeHostProcessMonitor m1, CodeHostProcessMonitor m2) {
                if (m1 == null) {
                    return m2 == null;
                } else if (m2 == null) {
                    return false; 
                }

                return
                    m1.PID == m2.PID &&
                    m1.StartTime == m2.StartTime &&
                    m1.IsMonitoringDatabase(m2.DatabaseName); 
            }

            public int GetHashCode(CodeHostProcessMonitor m) {
                var s = string.Format("{0}{1}{2}", m.DatabaseName, m.PID, m.StartTime.Ticks);
                return s.GetHashCode();
            }
        }

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

        public bool IsMonitoringDatabase(Database database) {
            return IsMonitoringDatabase(database.Name);
        }

        public bool IsMonitoringDatabase(string database) {
            return DatabaseName.Equals(database, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Cancel() {
            var wasCancelled = Cancelled == false;
            Cancelled = true;
            return wasCancelled;
        }

        /// <summary>
        /// Unregisters existing codehost.
        /// </summary>
        static void UnregisterCodehostInGateway(String dbName) {

            String reqBody =
                dbName + " ";

            reqBody += MixedCodeConstants.EndOfRequest;

            Response r = Http.DELETE(
                "http://localhost:" + StarcounterEnvironment.Default.SystemHttpPort + "/gw/codehost", reqBody, null);

            if (!r.IsSuccessStatusCode) {
                Trace("UnregisterCodehostInGateway: raising an exception in process event handler");

                String errCodeStr = r.Headers[MixedCodeConstants.ScErrorCodeHttpHeader];

                if (null != errCodeStr)
                    throw ErrorCode.ToException(UInt32.Parse(errCodeStr), r.Body);
                else
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, r.Body);
            }
        }

        public void CodeHostExited(object sender, EventArgs args) {
            // Unregistering codehost in gateway.
            UnregisterCodehostInGateway(DatabaseName);

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
                log.Debug("Ignoring synchronization of server state for {0}; monitoring was cancelled.", DatabaseName);
                return;
            }

            Database database;
            var databaseExist = server.Databases.TryGetValue(DatabaseName, out database);
            if (!databaseExist) {
                // Might have been deleted.
                // Take no action.
                log.Debug("Ignoring synchronization of server state for {0}; database was not found.", DatabaseName);
                return;
            }

            var boundProcess = Monitor.GetCodeHostProcess(database);
            if (boundProcess != null) {
                // The database is bound to some other process. We should
                // let it be.
                log.Debug(
                    "Ignoring synchronization of server state for {0}; it was restarted in process {1}", 
                    DatabaseName,
                    boundProcess.Id);
                return;
            }
            
            Monitor.ResetInternalAndPublicState(server.DatabaseEngine, database, process);
        }

        [Conditional("TRACE")]
        static void Trace(string message, bool restartWatch = false, params object[] args)
        {
            message = string.Format(message, args);
            Diagnostics.WriteTimeStamp(ServerLogSources.Commands.Source, message);
        }
    }
}
