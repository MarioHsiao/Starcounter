using System;
using System.Diagnostics;

namespace Starcounter.Logging
{
    public class LogSource
    {

        public LogSource(String source)
        {
        }

        public string Source
        {
            get
            {
                return null;
            }
        }

        [Conditional("DEBUG")]
        public void Debug(String message)
        {
        }

        [Conditional("DEBUG")]
        public void Debug(String message, params Object[] args)
        {
        }

        [Conditional("TRACE")]
        public void Trace(String message)
        {
        }

        [Conditional("TRACE")]
        public void Trace(String message, params Object[] args)
        {
        }

        public void LogNotice(String message)
        {
        }

        public void LogNotice(String message, params Object[] args)
        {
        }

        public void LogWarning(String message)
        {
        }

        public void LogWarning(String message, params Object[] args)
        {
        }

        public void LogError(String message)
        {
        }

        public void LogError(String message, params Object[] args)
        {
        }

        public void LogException(Exception exception)
        {
        }

        public void LogException(Exception exception, String message)
        {
        }

        public void LogException(Exception exception, String message, params Object[] args)
        {
        }

        public void LogCritical(String message)
        {
        }

        public void LogCritical(String message, params Object[] args)
        {
        }

        public void LogSuccessAudit(String message)
        {
        }

        public void LogSuccessAudit(String message, params Object[] args)
        {
        }

        public void LogFailureAudit(String message)
        {
        }

        public void LogFailureAudit(String message, params Object[] args)
        {
        }
    }
}
