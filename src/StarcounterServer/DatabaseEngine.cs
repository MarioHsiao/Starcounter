
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates and abstracts the database engine, i.e. letting code
    /// using this class act on the database engine without having to know
    /// the exact underlying details about how to actually start it or
    /// what exact input to use.
    /// </summary>
    internal sealed class DatabaseEngine {
        
        internal const string DatabaseExeFileName = "scpmm.exe";
        internal const string WorkerProcessExeFileName = "boot.exe";

        /// <summary>
        /// Gets the server that has instantiated this engine.
        /// </summary>
        internal readonly ServerNode Server;

        /// <summary>
        /// Gets the full path to the database executable.
        /// </summary>
        internal string DatabaseExePath {
            get;
            private set;
        }

        /// <summary>
        /// Gets the full path to the worker process executable.
        /// </summary>
        internal string WorkerProcessExePath {
            get;
            private set;
        }

        internal DatabaseEngine(ServerNode server) {
            this.Server = server;
        }

        internal void Setup() {
            var databaseExe = Path.Combine(this.Server.InstallationDirectory, DatabaseExeFileName);
            if (!File.Exists(databaseExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Database engine executable not found: {0}", databaseExe));
            }
            var workerProcExe = Path.Combine(this.Server.InstallationDirectory, WorkerProcessExeFileName);
            if (!File.Exists(workerProcExe)) {
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED, string.Format("Worker process executable not found: {0}", databaseExe));
            }

            this.DatabaseExePath = databaseExe;
            this.WorkerProcessExePath = workerProcExe;
        }

        internal ProcessStartInfo GetDatabaseStartInfo(Database database) {
            throw new NotImplementedException();
        }

        internal ProcessStartInfo GetWorkerProcessStartInfo(Database database) {
            throw new NotImplementedException();
        }
    }
}