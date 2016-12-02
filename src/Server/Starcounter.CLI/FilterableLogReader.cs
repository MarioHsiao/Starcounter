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
        DateTime sinceResolution;

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
        /// Filters logs fetched on a specified database.
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Specifies a filter that ignores any log entry
        /// older than the given value.
        /// </summary>
        public DateTime Since { 
            get { return sinceResolution; }
            set {
                sinceResolution = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Kind);
            }
        }

        /// <summary>
        /// Initialize a new <see cref="FilterableLogReader"/>.
        /// </summary>
        public FilterableLogReader() {
            Since = DateTime.MinValue;
        }

        /// <summary>
        /// Factory method creating a reader from a set of parameters.
        /// </summary>
        /// <param name="typeOfLogs">The type of logs to include.</param>
        /// <param name="since">Since-filter, specifying the oldest time of any
        /// log.</param>
        /// <returns>A reader scoped to the given arguments.</returns>
        public static FilterableLogReader LogsSince(Severity typeOfLogs, DateTime since) {
            return new FilterableLogReader() {
                Count = int.MaxValue,
                Since = since,
                TypeOfLogs = typeOfLogs
            };
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
            var since = sinceResolution;
            var type = TypeOfLogs;
            var source = Source;

            if (entry.DateTime < since) {
                stop = true;
                return true;
            }

            if (entry.Severity < type) return true;
            if (source != null) {
                if (source.EndsWith("*")) {
                    source = source.TrimEnd('*');
                    if (!entry.Source.StartsWith(source, StringComparison.InvariantCultureIgnoreCase)) {
                        return true;
                    }
                } else if (!entry.Source.Equals(source, StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
            }

            if (Database != null) {
                var uri = ScUri.FromString(entry.HostName);
                if (uri.Kind != ScUriKind.Database) {
                    return true;
                }

                if (!uri.DatabaseName.Equals(Database)) {
                    return true;
                }
            }

            return false;
        }

        static string GetLogDirectory() {
            var serverConfig = InstallationBasedServerConfigurationProvider.GetConfiguration();
            return serverConfig.LogDirectory;
        }
    }
}
