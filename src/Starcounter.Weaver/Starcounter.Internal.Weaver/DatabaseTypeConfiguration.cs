using System.Collections.Generic;
using System.IO;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Expose functionality that allows a client to ask if a given
    /// class is configured as a database type.
    /// </summary>
    public class DatabaseTypeConfiguration {
        static DatabaseTypeConfiguration empty = new DatabaseTypeConfiguration();
        
        public const string TypeConfigFileName = "db.types";

        List<string> configuredNamespaces = new List<string>();

        /// <summary>
        /// Gets the path to the configuration file used to populate the
        /// current configuration, or null if no such file was used.
        /// </summary>
        public string FilePath {
            get;
            private set;
        }

        private DatabaseTypeConfiguration() {
        }

        public static DatabaseTypeConfiguration Open(string directory) {
            var configFile = Path.Combine(directory, TypeConfigFileName);
            if (!File.Exists(configFile)) {
                return empty;
            }

            var config = new DatabaseTypeConfiguration();
            config.FilePath = configFile;

            var content = File.ReadAllLines(configFile);
            foreach (var line in content) {
                var trimmed = line.Trim();
                if (trimmed == string.Empty || trimmed.StartsWith("#")) {
                    continue;
                }
                config.configuredNamespaces.Add(trimmed);
            }

            return config;
        }

        public bool IsConfiguredDatabaseType(string fullTypeName) {
            var lastDot = fullTypeName.LastIndexOf('.');
            if (lastDot != -1) {
                var ns = fullTypeName.Substring(0, lastDot);
                return configuredNamespaces.Contains(ns);
            }
            return configuredNamespaces.Contains("*");
        }
    }
}
