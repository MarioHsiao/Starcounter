
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Starcounter.Configuration {
    
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