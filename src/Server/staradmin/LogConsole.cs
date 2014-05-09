using Sc.Tools.Logging;
using Starcounter.CLI;
using System;
using System.Text;

namespace staradmin {
    /// <summary>
    /// Governs the output of logs to be printed to the console.
    /// </summary>
    internal sealed class LogConsole {
        public bool ShowSourceAndHost { get; set; }
        public ConsoleColor HeaderColor { get; set; }

        public LogConsole() {
            ShowSourceAndHost = true;
            HeaderColor = ConsoleColor.DarkGray;
        }

        public void Write(LogEntry log) {
            var time = GetTimeString(log);
            var color = GetMessageColor(log);

            var header = new StringBuilder();
            header.AppendFormat("[{0}", time);
            if (ShowSourceAndHost) {
                header.AppendFormat(", {0} / {1}", log.Source, log.HostName);
            }
            header.Append("]");

            ConsoleUtil.ToConsoleWithColor(header.ToString(), HeaderColor);
            ConsoleUtil.ToConsoleWithColor(log.Message, color);
            Console.WriteLine();
        }

        static string GetTimeString(LogEntry log) {
            var x = log.DateTime;
            if (x.Date == DateTime.Today) {
                return x.TimeOfDay.ToString();
            }
            return x.ToString();
        }

        static ConsoleColor GetMessageColor(LogEntry log) {
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
