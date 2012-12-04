// ***********************************************************************
// <copyright file="sccorelog.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{

    /// <summary>
    /// Class sccorelog
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class sccorelog
    {

        /// <summary>
        /// The S c_ ENTR y_ DEBUG
        /// </summary>
        public const uint SC_ENTRY_DEBUG = 0;

        /// <summary>
        /// The S c_ ENTR y_ SUCCES s_ AUDIT
        /// </summary>
        public const uint SC_ENTRY_SUCCESS_AUDIT = 1;

        /// <summary>
        /// The S c_ ENTR y_ FAILUR e_ AUDIT
        /// </summary>
        public const uint SC_ENTRY_FAILURE_AUDIT = 2;

        /// <summary>
        /// The S c_ ENTR y_ NOTICE
        /// </summary>
        public const uint SC_ENTRY_NOTICE = 3;

        /// <summary>
        /// The S c_ ENTR y_ WARNING
        /// </summary>
        public const uint SC_ENTRY_WARNING = 4;

        /// <summary>
        /// The S c_ ENTR y_ ERROR
        /// </summary>
        public const uint SC_ENTRY_ERROR = 5;

        /// <summary>
        /// The S c_ ENTR y_ CRITICAL
        /// </summary>
        public const uint SC_ENTRY_CRITICAL = 6;

        /// <summary>
        /// SCs the init module_ LOG.
        /// </summary>
        /// <param name="hmenv">The hmenv.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint sccorelog_init(ulong hmenv);

        /// <summary>
        /// SCs the connect to logs.
        /// </summary>
        /// <param name="server_name">Name of the server.</param>
        /// <param name="ignore">The ignore.</param>
        /// <param name="phlogs">The phlogs.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe uint sccorelog_connect_to_logs(string server_name, void* ignore, ulong* phlogs);

        /// <summary>
        /// SCs the bind logs to dir.
        /// </summary>
        /// <param name="hlogs">The hlogs.</param>
        /// <param name="directory">The directory.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint sccorelog_bind_logs_to_dir(ulong hlogs, string directory);

        /// <summary>
        /// SCs the new activity.
        /// </summary>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint sccorelog_new_activity();

        /// <summary>
        /// SCs the write to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="type">The type.</param>
        /// <param name="source">The source.</param>
        /// <param name="category">The category.</param>
        /// <param name="message">The message.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint sccorelog_write_to_logs(ulong h, uint type, string source, string category, string message);

        /// <summary>
        /// SCs the kernel write to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="type">The type.</param>
        /// <param name="message">The message.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint sccorelog_kernel_write_to_logs(ulong h, uint type, string message);

        /// <summary>
        /// SCs the flush to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint sccorelog_flush_to_logs(ulong h);
    }
}
