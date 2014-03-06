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

        /// <summary>
        /// Managed callback to handle errors.
        /// </summary>
        /// <param name="err_code"></param>
        /// <param name="err_string"></param>
        internal static unsafe void ErrorHandlingCallbackFunc(
            UInt32 err_code,
            Char* err_string,
            Int32 err_string_len
            )
        {
            String managed_err_string = new String(err_string, 0, err_string_len);
            Exception exc = ErrorCode.ToException(err_code, managed_err_string);
            LogSources.Hosting.LogException(exc);
        }
    }
}
