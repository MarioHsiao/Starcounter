// ***********************************************************************
// <copyright file="ServerLogSources.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Logging;

namespace Starcounter.Server {

    /// <summary>
    /// Defines the names of well known server log sources.
    /// </summary>
    public static class ServerLogNames {
        /// <summary>
        /// Gets the name of the default log source.
        /// </summary>
        public const string Default = "Starcounter.Server";

        /// <summary>
        /// Gets the name of the process monitor log source.
        /// </summary>
        public const string Processes = "Starcounter.Server.Processes";

        /// <summary>
        /// Gets the name of the command processing log source.
        /// </summary>
        public const string Commands = "Starcounter.Server.Commands";

        /// <summary>
        /// Gets the name of the weaver log source.
        /// </summary>
        public const string Weaver = "Starcounter.Server.Weaver";
    }

    /// <summary>
    /// Contains references to the log sources used by the server.
    /// </summary>
    internal static class ServerLogSources {
        /// <summary>
        /// The default server log source.
        /// </summary>
        internal readonly static LogSource Default = new LogSource(ServerLogNames.Default);

        /// <summary>
        /// The log source used by the server to log information
        /// relating to external process management.
        /// </summary>
        internal readonly static LogSource Processes = new LogSource(ServerLogNames.Processes);

        /// <summary>
        /// The log source used by the server to log/trace
        /// messages that contains information about commands
        /// being executed (including their child tasks).
        /// </summary>
        internal readonly static LogSource Commands = new LogSource(ServerLogNames.Commands);

        /// <summary>
        /// The log source used by the server to log/trace
        /// messages that originates from the weaver process.
        /// </summary>
        internal readonly static LogSource Weaver = new LogSource(ServerLogNames.Weaver);
    }
}