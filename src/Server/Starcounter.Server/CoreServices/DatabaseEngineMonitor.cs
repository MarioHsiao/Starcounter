
using Starcounter.Server.Commands;
using Starcounter.Server.PublicModel.Commands;
using System.Diagnostics;

namespace Starcounter.Server {
    /// <summary>
    /// Governs the monitoring and reacting to database engine processes
    /// that exit.
    /// </summary>
    internal sealed class DatabaseEngineMonitor {
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
            
            engineProcess.EnableRaisingEvents = true;
            engineProcess.Exited += (sender, args) => {
                var x = new ActionCommand<Process, Database>(this.Server, ReactToProcessExit, engineProcess, database, description);
                this.Server.CurrentPublicModel.Execute(x);
            };
        }

        void ReactToProcessExit(ICommandProcessor processor, Process processExited, Database database) {
            // Implement the monitoring behaviour
            // TODO:
        }
    }
}