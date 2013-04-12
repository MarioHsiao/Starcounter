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

            uint e = 0;
            Exception current = ex;

            // Might be that the real exception is an inner exception wrapped in some 
            // other exception that does not contain the errorcode so we traverse all 
            // exceptions until we find an errorcode or no more innerexceptions exist.
            while (current != null) {
                if (ErrorCode.TryGetCode(current, out e))
                    break;
                current = current.InnerException;
            }

            if (e == 0) // No errorcode is found.
                e = 1;

            Kernel32.ExitProcess(e);
            return true;
        }
    }
}
