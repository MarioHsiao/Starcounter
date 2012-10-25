// ***********************************************************************
// <copyright file="ExceptionManager.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter;
using Starcounter.Internal;
using System;

namespace StarcounterInternal.Hosting
{

    /// <summary>
    /// Class ExceptionManager
    /// </summary>
    public static class ExceptionManager
    {

        /// <summary>
        /// Handles the unhandled exception.
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public static bool HandleUnhandledException(Exception ex)
        {
            string message = Starcounter.Logging.ExceptionFormatter.ExceptionToString(ex);

            sccoreapp.sccoreapp_log_critical_message(message);

            if (!Console.IsInputRedirected)
            {
                Console.Error.WriteLine(message);
            }

            uint e;
            if (!ErrorCode.TryGetCode(ex, out e)) e = 1;
            Kernel32.ExitProcess(e);

            return true;
        }
    }
}
