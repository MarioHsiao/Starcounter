using Sc.Tools.Logging;
using Starcounter.CLI;
using System;

namespace staradmin {
    /// <summary>
    /// Governs the output of logs to be printed to the console.
    /// </summary>
    internal static class LogConsole {

        public static void OutputLog(LogEntry log) {
            var time = GetTimeString(log);
            var color = GetColor(log);

            ConsoleUtil.ToConsoleWithColor(log.Message, color);
            ConsoleUtil.ToConsoleWithColor(
                string.Format("[{0}, {1} @ {2}]", time, log.Source, log.HostName), 
                ConsoleColor.DarkGray);
            Console.WriteLine();
        }

        static string GetTimeString(LogEntry log) {
            var x = log.DateTime;
            if (x.Date == DateTime.Today) {
                return x.TimeOfDay.ToString();
            }
            return x.ToString();
        }

        static ConsoleColor GetColor(LogEntry log) {
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
