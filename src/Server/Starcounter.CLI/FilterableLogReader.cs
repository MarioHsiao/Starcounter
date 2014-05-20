using Sc.Tools.Logging;
using Starcounter.Advanced.Configuration;
using Starcounter.Internal;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Starcounter.CLI {    
    using Severity = Sc.Tools.Logging.Severity;

    /// <summary>
    /// Provides functionality that allows a client to fetch log entries
    /// from the (local) Starcounter log, possibly applying some
    /// filtering.
    /// </summary>
    public sealed class FilterableLogReader {
        /// <summary>
        /// Number of log records to fetch.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Minimum severity of logs to consider.
        /// </summary>
        public Severity TypeOfLogs { get; set; }

        /// <summary>
        /// Filters logs fetched on a specified source.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Specifies a filter that ignores any log entry
        /// older than the given value.
        /// </summary>
        public DateTime Since { get; set; }

        /// <summary>
        /// Initialize a new <see cref="FilterableLogReader"/>.
        /// </summary>
        public FilterableLogReader() {
            Since = DateTime.MinValue;
        }

        /// <summary>
        /// Fetches a log entries from the server log and invokes the specified
        /// callback on every matching entry.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public int Fetch(Action<LogEntry> callback) {
            int read = 0;
            bool stop = false;
            int count = Count;
            
            var logDirectory = GetLogDirectory();
            var logReader = new LogReader();
            logReader.Open(logDirectory, ReadDirection.Reverse, 1024 * 32);
            try {
                while (read < count && !stop) {
                    var next = logReader.Next();
                    if (next == null) break;
                    if (FilterAway(next, ref stop)) continue;
                    callback(next);
                    read++;
                }
            } finally {
                logReader.Close();
            }

            return read;
        }

        bool FilterAway(LogEntry entry, ref bool stop) {
            var since = Since;
            var type = TypeOfLogs;
            var source = Source;

            if (entry.DateTime < since) {
                stop = true;
                return true;
            }

            if (entry.Severity < type) return true;
            if (source != null && !entry.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase))
                return true;

            return false;
        }

        static string GetLogDirectory() {
            var configDir = Path.Combine(StarcounterEnvironment.InstallationDirectory, StarcounterEnvironment.Directories.InstallationConfiguration);
            var configFile = Path.Combine(configDir, StarcounterEnvironment.FileNames.InstallationServerConfigReferenceFile);

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
