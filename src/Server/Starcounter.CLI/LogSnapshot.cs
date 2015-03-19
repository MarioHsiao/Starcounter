using Sc.Tools.Logging;
using Starcounter.Server;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.CLI {

    /// <summary>
    /// Expose predefined sets of logs captured from a given
    /// log reader, taken at a certain time.
    /// </summary>
    public sealed class LogSnapshot {
        List<LogEntry> entries;

        /// <summary>
        /// Gets the name of the database the current snapshot use
        /// to capture database/codehost-specific logs, retreived
        /// by <see cref="DatabaseLogs"/>.
        /// </summary>
        public readonly string Database;

        /// <summary>
        /// Gets the set of entries that are specific to a certain,
        /// named database/codehost, specified by <see cref="Database"/>.
        /// </summary>
        public LogEntry[] DatabaseLogs {
            get {
                List<LogEntry> result = null;
                if (!string.IsNullOrEmpty(Database)) {
                    result = entries.FindAll((candidate) => {
                        var uri = ScUri.FromString(candidate.HostName);
                        if (uri.Kind == ScUriKind.Database) {
                            return uri.DatabaseName.Equals(Database, StringComparison.InvariantCultureIgnoreCase);
                        }
                        return false;
                    });
                }

                return result == null ? new LogEntry[0] : result.ToArray();
            }
        }

        /// <summary>
        /// Gets the set of entries that originates from the
        /// weaver component.
        /// </summary>
        public LogEntry[] WeaverLogs {
            get {
                List<LogEntry> result = null;
                result = entries.FindAll((candidate) => {
                    return candidate.Source == ServerLogNames.Weaver;
                });
                return result == null ? new LogEntry[0] : result.ToArray();
            }
        }

        /// <summary>
        /// Get all entries captured.
        /// </summary>
        public LogEntry[] All {
            get {
                return entries.ToArray();
            }
        }

        private LogSnapshot(string database) {
            entries = new List<LogEntry>();
            Database = database;
        }

        /// <summary>
        /// Captures a snapshot using the given parameters.
        /// </summary>
        /// <param name="reader">The filterable log reader to use.</param>
        /// <param name="database">An optional database, allowing logs from
        /// only that host to be later retreived using <see cref="DatabaseLogs"/>
        /// </param>
        /// <returns>A snapshot based on the given parameters.</returns>
        public static LogSnapshot Take(FilterableLogReader reader, string database = null) {
            var snapshot = new LogSnapshot(database);
            reader.Fetch((entry) => { snapshot.entries.Add(entry); });
            return snapshot;
        }
    }
}
