
using System;

namespace Starcounter.Server.PublicModel {
    /// <summary>
    /// Represents the subset of a <see cref="DatabaseInfo"/> that
    /// contains information about the state of a possibly running
    /// database engine.
    /// </summary>
    public sealed class EngineInfo {

        /// <summary>
        /// Gets an opaque string that can be used to fingerprint
        /// any engine represented by an instance of this class.
        /// </summary>
        public readonly string Fingerprint;

        /// <summary>
        /// Gets the set of "Apps" currently hosted in the engine
        /// represented by this snapshot.
        /// </summary>
        public readonly AppInfo[] HostedApps;

        /// <summary>
        /// Gets the process ID of the database host process, if running.
        /// </summary>
        public readonly int HostProcessId;

        /// <summary>
        /// Gets a value representing the exact command-line
        /// arguments string what was used to start the host.
        /// </summary>
        public readonly string CodeHostArguments;

        /// <summary>
        /// Gets a value indicating if the database process is running.
        /// </summary>
        /// <remarks>
        /// The server intentionally don't reveal the PID or any other sensitive
        /// information about the database process, just letting server hosts
        /// know if it's running or not.
        /// <seealso cref="HostProcessId"/>
        /// </remarks>
        public readonly bool DatabaseProcessRunning;

        /// <summary>
        /// Initializes a <see cref="EngineInfo"/>.
        /// </summary>
        internal EngineInfo(AppInfo[] executables, int hostProcessId, string hostProcessArgs, bool databaseRunning) {
            this.HostedApps = executables;
            this.HostProcessId = hostProcessId;
            this.CodeHostArguments = hostProcessArgs;
            this.DatabaseProcessRunning = databaseRunning;

            // Derive a good-enough fingerprint for the current instance,
            // based on the current time. Alternative would be Guid.
            this.Fingerprint = DateTime.Now.Ticks.ToString().ToLowerInvariant();
        }
    }
}
