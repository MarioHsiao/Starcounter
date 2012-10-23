// ***********************************************************************
// <copyright file="Severity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Internal {
    /// <summary>
    /// Severity degrees for the Starcounter log.
    /// </summary>
    public enum Severity : uint {
        /// <summary>
        /// A debug log entry.
        /// </summary>
        Debug = sccorelog.SC_ENTRY_DEBUG,
        /// <summary>
        /// Security audit success, occurring for example when a user successfully
        /// has been logged in to the server.
        /// </summary>
        AuditSuccess = sccorelog.SC_ENTRY_SUCCESS_AUDIT,
        /// <summary>
        /// Security audit failure, occurring for example when a user failed logging
        /// on to the server because of invalid credentials.
        /// </summary>
        AuditFailure = sccorelog.SC_ENTRY_FAILURE_AUDIT,
        /// <summary>
        /// An informational log entry.
        /// </summary>
        Notice = sccorelog.SC_ENTRY_NOTICE,
        /// <summary>
        /// A warning log entry, signalling something odd and potentially dangerous,
        /// but not necessarily wrong.
        /// </summary>
        Warning = sccorelog.SC_ENTRY_WARNING,
        /// <summary>
        /// An error log entry, signalling a serious error which is likely to stop
        /// the server from functioning normally.
        /// </summary>
        Error = sccorelog.SC_ENTRY_ERROR,
        /// <summary>
        /// A critical log entry, signalling a critical failure which most likely
        /// will cause the server to shut down.
        /// </summary>
        Critical = sccorelog.SC_ENTRY_CRITICAL,
    }
}