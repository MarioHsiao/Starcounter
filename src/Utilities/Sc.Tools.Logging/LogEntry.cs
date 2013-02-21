
using System;

namespace Sc.Tools.Logging {

    public class LogEntry {

        public LogEntry(DateTime dateTime, Severity severity, string hostName, string source, int errorCode, string message) {
            DateTime = dateTime;
            Severity = severity;
            HostName = hostName;
            Source = source;
            ErrorCode = errorCode;
            Message = message;
        }

        public DateTime DateTime { get; private set; }
        public Severity Severity { get; private set; }
        public string HostName { get; private set; }
        public string Source { get; private set; }
        public int ErrorCode { get; private set; }
        public string Message { get; private set; }
    }
}
