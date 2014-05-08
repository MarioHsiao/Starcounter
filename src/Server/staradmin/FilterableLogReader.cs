using Sc.Tools.Logging;
using Starcounter.Advanced.Configuration;
using Starcounter.Internal;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace staradmin {    
    using Severity = Sc.Tools.Logging.Severity;

    /// <summary>
    /// Provides functionality that allows a client to fetch log entries
    /// from the (local) Starcounter log, possibly applying some
    /// filtering.
    /// </summary>
    internal sealed class FilterableLogReader {
        public const int DefaultNumberOfLogEntries = 25;

        /// <summary>
        /// Fetches a given number of log entries from the server log and
        /// invokes the specified callback on every matching entry.
        /// </summary>
        /// <param name="callback">The callback to invoke on each hit.</param>
        /// <param name="type">Specifies the severity to use as a lower bound.</param>
        /// <param name="count">Number of entries to fetch.</param>
        /// <param name="sourceFilter">Optional source to filter on</param>
        /// <returns>Number of entries actually fetched.</returns>
        public static int Fetch(Action<LogEntry> callback, Severity type = Severity.Warning, int count = DefaultNumberOfLogEntries, string sourceFilter = null) {
            int read = 0;
            var logDirectory = GetLogDirectory();
            var logReader = new LogReader();
            logReader.Open(logDirectory, ReadDirection.Reverse, 1024 * 32);
            try {

                while (read < count) {
                    var next = logReader.Next();
                    if (next == null) break;
                    if (IsPartOfResult(next, type, sourceFilter)) {
                        callback(next);
                        read++;
                    }
                }
            } finally {
                logReader.Close();
            }

            return read;
        }

        static bool IsPartOfResult(LogEntry entry, Severity type, string sourceFilter) {
            if (entry.Severity >= type) {
                return sourceFilter == null ? true : entry.Source.Equals(sourceFilter, StringComparison.InvariantCultureIgnoreCase);
            }
            return false;
        }

        static string GetLogDirectory() {
            var installationDir = StarcounterEnvironment.InstallationDirectory;
            var configFile = Path.Combine(installationDir, "Personal.xml");

            var xml = XDocument.Load(configFile);
            var query = from c in xml.Root.Descendants("server-dir")
                        select c.Value;
            var serverDir = query.First();
            var serverConfigPath = Path.Combine(serverDir, "Personal" + ServerConfiguration.FileExtension);
            
            var serverConfig = ServerConfiguration.Load(serverConfigPath);
            return serverConfig.LogDirectory;
        }
    }
}
