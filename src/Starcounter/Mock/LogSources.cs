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

        /// <summary>
        /// Log sources used by the Starcounter code host loader.
        /// </summary>
        public static LogSource CodeHostLoader = new LogSource("Starcounter.Host.Loader");

        /// <summary>
        /// Log sources used by the Starcounter code host assembly resolver.
        /// </summary>
        public static LogSource CodeHostAssemblyResolver = new LogSource("Starcounter.Host.AssemblyResolver");

        /// <summary>
        /// Well-known log source used by the database host when unloading.
        /// </summary>
        public static LogSource Unload = new LogSource("Starcounter.Host.Unload");
    }
}
