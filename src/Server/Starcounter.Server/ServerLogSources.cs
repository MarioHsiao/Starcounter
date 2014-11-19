// ***********************************************************************
// <copyright file="ServerLogSources.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Logging;

namespace Starcounter.Server {

    /// <summary>
    /// Contains references to the log sources used by the server.
    /// </summary>
    internal static class ServerLogSources {
        /// <summary>
        /// The default server log source.
        /// </summary>
        internal readonly static LogSource Default = new LogSource("Starcounter.Server");

        /// <summary>
        /// The log source used by the server to log information
        /// relating to external process management.
        /// </summary>
        internal readonly static LogSource Processes = new LogSource("Starcounter.Server.Processes");

        /// <summary>
        /// The log source used by the server to log/trace
        /// messages that contains information about commands
        /// being executed (including their child tasks).
        /// </summary>
        internal readonly static LogSource Commands = new LogSource("Starcounter.Server.Commands");
    }
}