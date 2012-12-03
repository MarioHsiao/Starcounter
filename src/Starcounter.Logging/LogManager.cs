
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

        public static void InternalFatal(String message)
        {
            sccorelog.sccorelog_kernel_write_to_logs(_hlogs, sccorelog.SC_ENTRY_CRITICAL, message);
            sccorelog.sccorelog_flush_to_logs(_hlogs);
        }

        internal static void Debug(String source, String message, String category, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_DEBUG, source, message, category, exception);
        }

        internal static void SuccessAudit(String source, String message, String category)
        {
            WriteToLogs(sccorelog.SC_ENTRY_SUCCESS_AUDIT, source, message, category, null);
        }

        internal static void FailureAudit(String source, String message, String category)
        {
            WriteToLogs(sccorelog.SC_ENTRY_FAILURE_AUDIT, source, message, category, null);
        }

        internal static void Notice(String source, String message, String category, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_NOTICE, source, message, category, exception);
        }

        internal static void Warning(String source, String message, String category, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_WARNING, source, message, category, exception);
        }

        internal static void Error(String source, String message, String category, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_ERROR, source, message, category, exception);
        }

        internal static void Critical(String source, String message, String category, Exception exception)
        {
            WriteToLogs(sccorelog.SC_ENTRY_CRITICAL, source, message, category, exception);
        }
        
        private static void WriteToLogs(uint type, string source, string message, string category, Exception exception)
        {
            string message2;
            if (exception != null)
            {
                message2 = ExceptionFormatter.ExceptionToString(exception);
                if (message == null)
                {
                    message = message2;
                }
                else
                {
                    message = string.Concat(message, " ", message2);
                }
            }
            sccorelog.sccorelog_write_to_logs(_hlogs, type, source, category, message);
        }
    }
}
