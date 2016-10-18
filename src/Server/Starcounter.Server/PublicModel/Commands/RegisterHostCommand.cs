
using System;

namespace Starcounter.Server.PublicModel.Commands
{
    /// <summary>
    /// Command issued when a host need to be registered with the server.
    /// </summary>
    public sealed class RegisterHostCommand : ServerCommand
    {
        /// <summary>
        /// Name of the database the host run on top.
        /// </summary>
        public string DatabaseName { get; private set; }

        /// <summary>
        /// The process ID of the host.
        /// </summary>
        public int ProcessId { get; private set; }

        /// <summary>
        /// Name of a hosted application. If set, indicating the
        /// host is a single-app self host.
        /// </summary>
        public string HostedApplicationName { get; set; }

        /// <summary>
        /// Initializes a new <see cref="RegisterHostCommand"/>.
        /// </summary>
        /// <param name="engine">The server engine</param>
        /// <param name="databaseName">Name of the database the host run on top.</param>
        public RegisterHostCommand(
            ServerEngine engine, 
            string databaseName,
            int processId,
            string applicationName) : base(engine, "Registering host {0}", databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }

            DatabaseName = databaseName;
            ProcessId = processId;
            HostedApplicationName = applicationName;
        }
    }
}