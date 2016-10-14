// ***********************************************************************
// <copyright file="sccoreapp.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace StarcounterInternal.Hosting
{

    /// <summary>
    /// Class sccoreapp
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class sccoreapp
    {

        /// <summary>
        /// Sccoreapp_inits the specified hlogs.
        /// </summary>
        /// <param name="hlogs">The hlogs.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoreapp_init(void* hlogs);

        /// <summary>
        /// Sccoreapp_standbies the specified hsched.
        /// </summary>
        /// <param name="hsched">The hsched.</param>
        /// <param name="ptask_data">The ptask_data.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern unsafe uint sccoreapp_standby(void* hsched, sccorelib.CM2_TASK_DATA* ptask_data);

        /// <summary>
        /// Sccoreapp_log_critical_codes the specified e.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe uint sccoreapp_log_critical_code(uint e);

        /// <summary>
        /// Sccoreapp_log_critical_messages the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>System.UInt32.</returns>
        [DllImport("sccoreapp.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern unsafe uint sccoreapp_log_critical_message(string message);
    }
}
