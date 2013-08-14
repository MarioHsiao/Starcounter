
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter.Logging
{
    public class LogSource :ILogSource
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
            LogManager.Debug(_source, message, null);
        }

        [Conditional("DEBUG")]
        public void Debug(string message, params object[] args)
        {
            LogManager.Debug(_source, string.Format(message, args), null);
        }

        [Conditional("TRACE")]
        public void Trace(string message)
        {
            LogManager.Debug(_source, message, null);
        }

        [Conditional("TRACE")]
        public void Trace(string message, params object[] args)
        {
            LogManager.Debug(_source, string.Format(message, args), null);
        }

        public void LogNotice(string message)
        {
            LogManager.Notice(_source, message, null);
        }

        public void LogNotice(string message, params object[] args)
        {
            LogManager.Notice(_source, string.Format(message, args), null);
        }

        public void LogWarning(string message)
        {
            LogManager.Warning(_source, message, null);
        }

        public void LogWarning(string message, params object[] args)
        {
            LogManager.Warning(_source, string.Format(message, args), null);
        }

        public void LogError(string message)
        {
            LogManager.Error(_source, message, null);
        }

        public void LogError(string message, params object[] args)
        {
            LogManager.Error(_source, string.Format(message, args), null);
        }

        public void LogException(Exception exception)
        {
            LogManager.Error(_source, null, exception);
        }

        public void LogException(Exception exception, string message)
        {
            LogManager.Error(_source, message, exception);
        }

        public void LogException(Exception exception, string message, params object[] args)
        {
            LogManager.Error(_source, string.Format(message, args), exception);
        }

        public void LogCritical(string message)
        {
            LogManager.Critical(_source, message, null);
        }

        public void LogCritical(string message, params object[] args)
        {
            LogManager.Critical(_source, string.Format(message, args), null);
        }

        public void LogSuccessAudit(string message)
        {
            LogManager.SuccessAudit(_source, message);
        }

        public void LogSuccessAudit(string message, params object[] args)
        {
            LogManager.SuccessAudit(_source, string.Format(message, args));
        }

        public void LogFailureAudit(string message)
        {
            LogManager.FailureAudit(_source, message);
        }

        public void LogFailureAudit(string message, params object[] args)
        {
            LogManager.FailureAudit(_source, string.Format(message, args));
        }
    }
}
