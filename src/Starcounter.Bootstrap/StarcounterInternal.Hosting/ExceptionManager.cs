using Starcounter.Bootstrap.RuntimeHosts;
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
            // Legacy call. Forward to currently installed dito.
            var installed = RuntimeHost.Current.ExceptionManager;
            return installed != null ? installed.HandleUnhandledException(ex) : false;
        }
    }
}
