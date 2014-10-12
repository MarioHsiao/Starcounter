
using Starcounter.Internal;
using System;

namespace Starcounter.Logging
{
    
    public static class LogManager
    {

        private static ulong _hlogs;

        public static void Setup(ulong hlogs)
        {
            _hlogs = hlogs;
        }

        public static void InternalFatal(uint errorCode, string message)
        {
            sccorelog.star_kernel_write_to_logs(_hlogs, sccorelog.SC_ENTRY_CRITICAL, errorCode, message);
            sccorelog.star_flush_to_logs(_hlogs);
        }

        internal static void Debug(String source, String message, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_DEBUG, source, message, exception);
        }

        internal static void SuccessAudit(String source, String message)
        {
            WriteToLogs(sccorelog.SC_ENTRY_SUCCESS_AUDIT, source, message, null);
        }

        internal static void FailureAudit(String source, String message)
        {
            WriteToLogs(sccorelog.SC_ENTRY_FAILURE_AUDIT, source, message, null);
        }

        internal static void Notice(String source, String message, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_NOTICE, source, message, exception);
        }

        internal static void Warning(String source, String message, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_WARNING, source, message, exception);
        }

        internal static void Error(String source, String message, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_ERROR, source, message, exception);
        }

        internal static void Critical(String source, String message, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_CRITICAL, source, message, exception);
        }
        
        private static void WriteToLogs(uint type, string source, string message, Exception exception)
        {
            uint errorCode = 0;
            if (exception != null)
            {
                ErrorCode.TryGetCode(exception, out errorCode);                
                var message2 = ExceptionFormatter.ExceptionToString(exception);
                if (message == null)
                {
                    message = message2;
                }
                else
                {
                    message = string.Concat(message, " ", message2);
                }
            }

            if (0 != _hlogs)
                sccorelog.star_write_to_logs(_hlogs, type, source, errorCode, message);
        }
    }
}
