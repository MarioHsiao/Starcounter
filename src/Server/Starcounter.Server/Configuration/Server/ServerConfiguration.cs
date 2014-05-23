// ***********************************************************************
// <copyright file="ServerConfiguration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.Advanced.Configuration {

    /// <summary>
    /// Configures the server.
    /// </summary>
    [XmlRoot("Server", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public class ServerConfiguration : ConfigurationFile {
        /// <summary>
        /// Extension of server configuration files (including the leading period).
        /// </summary>
        public const string FileExtension = ".server.config";

        /// <summary>
        /// Initializes a new <see cref="ServerConfiguration"/>.
        /// </summary>
        public ServerConfiguration() {
        }

        /// <summary>
        /// Initializes a new <see cref="ServerConfiguration"/>
        /// and specifies the file name.
        /// </summary>
        /// <param name="fileName">Name of the configuration file.</param>
        public ServerConfiguration(string fileName)
            : base(fileName) {
        }

        /// <inheritdoc />
        public override string GetFileExtension() {
            return FileExtension;
        }

        /// <summary>
        /// Path of the directory containing databases hosted by this server.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string DatabaseDirectory {
            get;
            set;
        }

        /// <summary>
        /// Path of the directory containing engine configuration files.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string EnginesDirectory {
            get;
            set;
        }

        /// <summary>
        /// Path of the temporary directory;
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string TempDirectory {
            get;
            set;
        }

        /// <summary>
        /// Path of the directory used to store log files.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public string LogDirectory {
            get;
            set;
        }

        /// <summary>
        /// Tcp port number for Administrator.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public UInt16 SystemHttpPort {
            get;
            set;
        }

        /// <summary>
        /// String representation.
        /// </summary>
        public static String SystemHttpPortString {
            get { return "SystemHttpPort"; }
        }

        /// <summary>
        /// Send usage statistics and crash reports to the tracker
        /// </summary>
        public bool SendUsageAndCrashReports {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the default database storage properties
        /// for the server to use when creating new databases and
        /// no values are explicitly given.
        /// </summary>
        [XmlElement(IsNullable = false)]
        public DatabaseStorageConfiguration DefaultDatabaseStorageConfiguration {
            get;
            set;
        }

        ///// <summary>
        ///// Gets or sets the default database maximum image size for the
        ///// server to use when creating new databases and no maximum image
        ///// size is explicitly given.
        ///// </summary>
        ///// <remarks>
        ///// Expressed in megabytes.
        ///// </remarks>
        //public long? DatabaseDefaultMaxImageSize {
        //    get;
        //    set;
        //}

        ///// <summary>
        ///// Gets or sets the default database transaction log size for the
        ///// server to use when creating new databases and no transaction log
        ///// size is explicitly given.
        ///// </summary>
        ///// <remarks>
        ///// Expressed in megabytes.
        ///// </remarks>
        //public long? DatabaseDefaultTransactionLogSize {
        //    get;
        //    set;
        //}

        /// <summary>
        /// Default configuration for instances of this engine.
        /// </summary>
        /// <remarks>
        /// Most instance-level parameters are optional, because the value is
        /// inferred from the default value by default. However, since the
        /// object in this property contains the default values, all its parameters
        /// are required.
        /// </remarks>
        [XmlElement(IsNullable = false)]
        public DatabaseConfiguration DefaultDatabaseConfiguration {
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

        /// <summary>
        /// Gets the full database directory path after any substitions
        /// have been made, resolved with the help of the current file if
        /// the configured path is relative.
        /// </summary>
        /// <returns>Full, resolved path to the database directory.
        /// </returns>
        public string GetResolvedDatabaseDirectory() {
            var databaseDirectory = Environment.ExpandEnvironmentVariables(DatabaseDirectory);
            if (!Path.IsPathRooted(databaseDirectory)) {
                var serverRepositoryDirectory = Path.GetDirectoryName(Path.GetFullPath(ConfigurationFilePath));
                databaseDirectory = Path.Combine(serverRepositoryDirectory, databaseDirectory);
            }
            return Path.GetFullPath(databaseDirectory);
        }

        /// <summary>
        /// Gets the full temporary directory path after any substitions
        /// have been made, resolved with the help of the current file if
        /// the configured path is relative.
        /// </summary>
        /// <returns>Full, resolved path to the temporary directory.
        /// </returns>
        public string GetResolvedTempDirectory() {
            var tempDirectory = Environment.ExpandEnvironmentVariables(TempDirectory);
            if (!Path.IsPathRooted(tempDirectory)) {
                var serverRepositoryDirectory = Path.GetDirectoryName(Path.GetFullPath(ConfigurationFilePath));
                tempDirectory = Path.Combine(serverRepositoryDirectory, tempDirectory);
            }
            return Path.GetFullPath(tempDirectory);
        }

        /// <summary>
        /// Loads an <see cref="ServerConfiguration"/> from a file.
        /// </summary>
        /// <param name="fileName">Name of the file containing the serialized <see cref="ServerConfiguration"/>.</param>
        /// <returns>The <see cref="ServerConfiguration"/> built from the file named <paramref name="fileName"/>.</returns>
        public static ServerConfiguration Load(string fileName) {
            return Load<ServerConfiguration>(fileName);
        }

        /// <summary>
        /// Returns a deep clone of the current instance.
        /// </summary>
        /// <returns>A deep clone of the current instance.</returns>
        public new ServerConfiguration Clone() {
            return (ServerConfiguration)base.Clone();
        }
    }
}