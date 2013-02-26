
using System;
using System.Diagnostics;

namespace Starcounter.Logging {

    /// <summary>
    /// Implements a trace listener that logs every traced message to
    /// the Starcounter server log, using a certain <see cref="LogSource"/>.
    /// </summary>
    public class LogTraceListener : TraceListener {

        /// <summary>
        /// The <see cref="LogSource"/> used by the current trace listener
        /// when a message comes along that should be logged.
        /// </summary>
        public readonly LogSource Log;

        /// <summary>
        /// Initialize a new <see cref="LogTraceListener"/> with a default
        /// log source name, based on the name of the current process.
        /// </summary>
        public LogTraceListener() {
            var name = Process.GetCurrentProcess().ProcessName;
            name = string.Concat(name, ".", "TraceLogger");
            this.Log = new LogSource(name);
        }

        /// <summary>
        /// Initialize a new <see cref="LogTraceListener"/>.
        /// </summary>
        /// <param name="source">The <see cref="LogSource"/> to be used
        /// by the current trace listener when a message is logged.</param>
        public LogTraceListener(LogSource source) {
            if (source == null)
                throw new ArgumentNullException("source");

            this.Log = source;
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the underlying log.
        /// </summary>
        /// <param name="message">The message being logged.</param>
        public override void Write(string message) {
            WriteLine(message);
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the underlying log.
        /// </summary>
        /// <param name="message">The message being logged.</param>
        public override void WriteLine(string message) {
            Log.Trace(message);
        }
    }
}