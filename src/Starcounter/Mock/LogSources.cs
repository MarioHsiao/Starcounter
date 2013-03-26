// ***********************************************************************
// <copyright file="LogSources.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Logging;

namespace Starcounter
{
    /// <summary>
    /// Class LogSources
    /// </summary>
    public class LogSources
    {
        /// <summary>
        /// The SQL
        /// </summary>
        public static LogSource Sql = new LogSource("Sql");

        /// <summary>
        /// Well-known log source used by the database host.
        /// </summary>
        public static LogSource Hosting = new LogSource("Starcounter.Host");
    }
}
