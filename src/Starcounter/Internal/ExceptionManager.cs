// ***********************************************************************
// <copyright file="ExceptionManager.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;

namespace Starcounter.Internal
{

    /// <summary>
    /// Class ExceptionManager
    /// </summary>
    internal static class ExceptionManager
    {
        /// <summary>
        /// Handles the internal fatal error.
        /// </summary>
        /// <param name="code">The code.</param>
        public static void HandleInternalFatalError(uint code)
        {
            string stackTrace;
            string message;
            try
            {
                try
                {
                    stackTrace = new StackTrace(1, true).ToString();
                }
                catch (Exception)
                {
                    stackTrace = "Failed to evaluate stack trace.";
                }
                message = Starcounter.ErrorCode.ToMessage(
                              "Fatal error detected:",
                              code,
                              stackTrace
                          );
                Starcounter.Logging.LogManager.InternalFatal(code, message);
            }
            finally
            {
                Kernel32.ExitProcess(code);
            }
        }
    }
}
