// ***********************************************************************
// <copyright file="DatabaseConfiguration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace Starcounter.Configuration {

    /// <summary>
    /// Configuration of a database instance.
    /// </summary>
    [XmlRoot("Database", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public class DatabaseConfiguration : ConfigurationFile {
        /// <summary>
        /// Initializes a new <see cref="DatabaseConfiguration"/>.
        /// </summary>
        public DatabaseConfiguration() {
            this.Monitoring = new MonitoringConfiguration();
        }

        /// <summary>
        /// Extension of database instance configuration files.
        /// </summary>
        public const string FileExtension = ".db.config";


        private DatabaseRuntimeConfiguration _runtime;
        /// <summary>
        /// Configuration of the database runtime.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public DatabaseRuntimeConfiguration Runtime {
            get {
                return _runtime;
            }
            set {
                _runtime = value;
                this.OnPropertyChanged("Runtime");
            }
        }

        /// <summary>
        /// Monitoring options.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public MonitoringConfiguration Monitoring {
            get;
            set;
        }

        /// <summary>
        /// Configuration of trace sources.
        /// </summary>
        [DefaultValue(null)]
        public List<TraceSourceConfiguration> TraceSources {
            get;
            set;
        }

        /// <inheritdoc />
        public override string GetFileExtension() {
            return FileExtension;
        }

        /// <summary>
        /// Loads an <see cref="DatabaseConfiguration"/> from
        /// a file on disk.
        /// </summary>
        /// <param name="fileName">Name of the file to be loaded.</param>
        /// <returns>The <see cref="DatabaseConfiguration"/>
        /// loaded from <paramref name="fileName"/>.</returns>
        public static DatabaseConfiguration Load(string fileName) {
            return Load<DatabaseConfiguration>(fileName);
        }

        /// <summary>
        /// Loads an <see cref="DatabaseConfiguration"/> from a <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The source stream.</param>
        /// <param name="fileName">Logical name of the file corresponding to that stream.</param>
        /// <returns>An <see cref="DatabaseConfiguration"/> built from <paramref name="stream"/>.</returns>
        public static DatabaseConfiguration Load(Stream stream, string fileName) {
            return Load<DatabaseConfiguration>(stream, fileName);
        }

        /// <summary>
        /// Returns a deep clone of the current <see cref="DatabaseConfiguration"/>
        /// and assigns it a file name.
        /// </summary>
        /// <param name="configurationFilePath">File name to be assigned to the clone.</param>
        /// <returns>The clone.</returns>
        public DatabaseConfiguration Clone(string configurationFilePath) {
            DatabaseConfiguration clone = (DatabaseConfiguration)Clone();
            clone.ConfigurationFilePath = configurationFilePath;
            return clone;
        }
    }
}