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
        /// </summary>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint sccorelog_init();

        /// <summary>
        /// SCs the connect to logs.
        /// </summary>
        /// <param name="host_name">Host name.</param>
        /// <param name="ignore">The ignore.</param>
        /// <param name="phlogs">The phlogs.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe uint star_connect_to_logs(string host_name, string directory, void* ignore, ulong* phlogs);

        /// <summary>
        /// SCs the write to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="type">The type.</param>
        /// <param name="source">The source.</param>
        /// <param name="error_code"></param>
        /// <param name="message">The message.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_write_to_logs(ulong h, uint type, string source, uint error_code, string message);

        /// <summary>
        /// SCs the kernel write to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <param name="type">The type.</param>
        /// <param name="error_code"></param>
        /// <param name="message">The message.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern uint star_kernel_write_to_logs(ulong h, uint type, uint error_code, string message);

        /// <summary>
        /// SCs the flush to logs.
        /// </summary>
        /// <param name="h">The h.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccorelog.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern uint star_flush_to_logs(ulong h);
    }
}
