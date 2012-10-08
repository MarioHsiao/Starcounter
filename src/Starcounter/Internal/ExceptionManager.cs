
using System;
using System.Diagnostics;

namespace Starcounter.Internal
{
    
    internal static class ExceptionManager
    {

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
                Starcounter.Logging.LogManager.InternalFatal(message);
            }
            finally
            {
                Kernel32.ExitProcess(code);
            }
        }
    }
}
