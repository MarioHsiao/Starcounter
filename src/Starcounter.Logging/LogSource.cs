
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter.Logging
{
    public class LogSource
    {

        private readonly string _source;

        public LogSource(string source)
        {
            _source = source;
        }

        public string Source { get { return _source; } }

        [Conditional("DEBUG")]
        public void Debug(string message)
        {
            LogManager.Debug(_source, message, null, null);
        }

        [Conditional("DEBUG")]
        public void Debug(string message, params object[] args)
        {
            LogManager.Debug(_source, string.Format(message, args), null, null);
        }

        [Conditional("TRACE")]
        public void Trace(string message)
        {
            LogManager.Debug(_source, message, null, null);
        }

        [Conditional("TRACE")]
        public void Trace(string message, params object[] args)
        {
            LogManager.Debug(_source, string.Format(message, args), null, null);
        }

        public void LogNotice(string message)
        {
            LogManager.Notice(_source, message, null, null);
        }

        public void LogNotice(string message, params object[] args)
        {
            LogManager.Notice(_source, string.Format(message, args), null, null);
        }

        public void LogWarning(string message)
        {
            LogManager.Warning(_source, message, null, null);
        }

        public void LogWarning(string message, params object[] args)
        {
            LogManager.Warning(_source, string.Format(message, args), null, null);
        }

        public void LogError(string message)
        {
            LogManager.Error(_source, message, null, null);
        }

        public void LogError(string message, params object[] args)
        {
            LogManager.Error(_source, string.Format(message, args), null, null);
        }

        public void LogException(Exception exception)
        {
            LogManager.Error(_source, null, null, exception);
        }

        public void LogException(Exception exception, string message)
        {
            LogManager.Error(_source, message, null, exception);
        }

        public void LogException(Exception exception, string message, params object[] args)
        {
            LogManager.Error(_source, string.Format(message, args), null, exception);
        }

        public void LogCritical(string message)
        {
            LogManager.Critical(_source, message, null, null);
        }

        public void LogCritical(string message, params object[] args)
        {
            LogManager.Critical(_source, string.Format(message, args), null, null);
        }

        public void LogSuccessAudit(string message)
        {
            LogManager.SuccessAudit(_source, message, null);
        }

        public void LogSuccessAudit(string message, params object[] args)
        {
            LogManager.SuccessAudit(_source, string.Format(message, args), null);
        }

        public void LogFailureAudit(string message)
        {
            LogManager.FailureAudit(_source, message, null);
        }

        public void LogFailureAudit(string message, params object[] args)
        {
            LogManager.FailureAudit(_source, string.Format(message, args), null);
        }
    }
}
