using Sc.Tools.Logging;
using Starcounter;
using Starcounter.CLI;
using System;
using System.Text;

namespace Starcounter.CLI {
    /// <summary>
    /// Governs the output of logs to be printed to the console.
    /// </summary>
    public sealed class LogConsole {
        /// <summary>
        /// Indicates if the source and the host should be part of
        /// the output.
        /// </summary>
        public bool ShowSourceAndHost { get; set; }

        /// <summary>
        /// Specifies the color of the header.
        /// </summary>
        public ConsoleColor HeaderColor { get; set; }

        /// <summary>
        /// Dictates if the console should output the host information
        /// in a simplfied form, as oppopsed to the "native" URI format
        /// using in the log.
        /// </summary>
        public bool ShowSimplifiedHost { get; set; }

        /// <summary>
        /// Gets or sets a value that cause the severity of each log
        /// entry written to be part of the header.
        /// </summary>
        public bool IncludeSeverityInHeader { get; set; }

        /// <summary>
        /// Initialize a new <see cref="LogConsole"/>.
        /// </summary>
        public LogConsole() {
            ShowSourceAndHost = true;
            ShowSimplifiedHost = true;
            HeaderColor = ConsoleColor.DarkGray;
            IncludeSeverityInHeader = Console.IsOutputRedirected;
        }

        /// <summary>
        /// Writes the given log entry to the console, formatted using
        /// the settings of the current instance.
        /// </summary>
        /// <param name="log">The log entry to write.</param>
        public void Write(LogEntry log) {
            var time = GetTimeString(log);
            var color = GetMessageColor(log);

            var header = new StringBuilder();
            header.AppendFormat("[{0}", time);
            if (IncludeSeverityInHeader) {
                header.AppendFormat(", {0}", log.Severity.ToString());
            }
            if (ShowSourceAndHost) {
                header.AppendFormat(", {0} ({1})", log.Source, GetHostString(log));
            }
            
            header.Append("]");

            ConsoleUtil.ToConsoleWithColor(header.ToString(), HeaderColor);
            ConsoleUtil.ToConsoleWithColor(log.Message, color);
            Console.WriteLine();
        }

        string GetTimeString(LogEntry log) {
            var x = log.DateTime;
            if (x.Date == DateTime.Today) {
                return x.TimeOfDay.ToString();
            }
            return x.ToString();
        }

        string GetHostString(LogEntry log) {
            var result = log.HostName;
            if (ShowSimplifiedHost) {
                try {
                    var uri = ScUri.FromString(log.HostName);
                    if (uri.Kind == ScUriKind.Database) {
                        return uri.DatabaseName;
                    } else if (uri.Kind == ScUriKind.Server) {
                        return "scadminserver";
                    }
                } catch { }
            }
            return result;
        }

        ConsoleColor GetMessageColor(LogEntry log) {
            switch (log.Severity) {
                case Severity.Debug:
                    return ConsoleColor.Gray;
                case Severity.Notice:
                    return ConsoleColor.Yellow;
                case Severity.Warning:
                    return ConsoleColor.Magenta;
                case Severity.Error:
                case Severity.Critical:
                    return ConsoleColor.Red;
                default:
                    return Console.ForegroundColor;
            }
        }
    }
}
