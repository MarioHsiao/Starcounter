// ***********************************************************************
// <copyright file="ConfigurationFile.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Base class for all configuration files.
    /// </summary>
    [XmlType(Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public abstract class ConfigurationFile : ConfigurationElement {
        private string configurationFilePath;

        /// <summary>
        /// Initializes a new <see cref="ConfigurationFile"/>.
        /// </summary>
        protected ConfigurationFile() {
        }

        /// <summary>
        /// Initializes a new <see cref="ConfigurationFile"/>
        /// and specifies the corresponding file name.
        /// </summary>
        /// <param name="fileName"></param>
        protected ConfigurationFile(string fileName) {
            this.ConfigurationFilePath = fileName;
        }

        /// <inheritdoc />
        protected override void CopyTo(ConfigurationElement clone) {
            base.CopyTo(clone);
            ((ConfigurationFile)clone).configurationFilePath = null;
        }

        /// <summary>
        /// Gets the desired file extension for the current kind of
        /// configuration file.
        /// </summary>
        /// <returns>A file extension including the training dot.</returns>
        public abstract string GetFileExtension();

        /// <summary>
        /// Name of the configuration element, i.e. the configuration file name without extension.
        /// </summary>
        /// <remarks>
        /// This property is XML serializable so that it can be serialized
        /// for remote communication. However, it should not be stored to files.
        /// </remarks>
        [XmlIgnore]
        public string Name {
            get;
            private set;
        }


        /// <summary>
        /// Gets or sets the path of the current configuration file.
        /// </summary>
        [XmlIgnore]
        public string ConfigurationFilePath {
            get {
                return this.configurationFilePath;
            }
            set {
                if (value == null) {
                    this.configurationFilePath = null;
                } else {
                    if (!value.ToLowerInvariant().EndsWith(this.GetFileExtension().ToLowerInvariant()))
                        throw new ArgumentOutOfRangeException("value",
                                                              string.Format(
                                                                  "The file name '{0}' should have the extension '{1}'",
                                                                  value, this.GetFileExtension()));
                    this.configurationFilePath = value;
                    string name = Path.GetFileName(value);
                    this.Name = name.Substring(0, name.Length - this.GetFileExtension().Length).ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Saves the current configuration file.
        /// </summary>
        public void Save() {
            this.Save(this.configurationFilePath);
        }


        /// <summary>
        /// Saves the current configuration file to disk.
        /// </summary>
        public void Save(string fileName) {
            if (fileName == null) {
                throw new ArgumentNullException();
            }
            string backupFile;
            // Backup the file.
            if (File.Exists(fileName)) {
                backupFile = fileName + ".bak";
                File.Copy(fileName, backupFile, true);
            } else {
                backupFile = null;
            }
            try {
                using (
                    XmlTextWriter writer = new XmlTextWriter(fileName, Encoding.UTF8)
                ) {
                    writer.Formatting = Formatting.Indented;
                    XmlSerializer serializer = new XmlSerializer(this.GetType());
                    serializer.Serialize(writer, this);
                    this.ConfigurationFilePath = fileName;
                }
            } catch {
                // Restore the file.
                if (backupFile != null) {
                    File.Copy(backupFile, fileName, true);
                } else {
                    File.Delete(fileName);
                }
                throw;
            }
        }

        /// <summary>
        /// Loads and parses a configuration file.
        /// </summary>
        /// <typeparam name="T">Type of configuration file.</typeparam>
        /// <param name="fileName">File name.</param>
        /// <returns>The object representing the configuration file.</returns>
        protected static T Load<T>(string fileName)
        where T : ConfigurationFile {
            T configurationFile;
            using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                configurationFile = Load<T>(stream, Path.GetFullPath(fileName));
            }
            return configurationFile;
        }

        /// <summary>
        /// Loads and parses a configuration file from a <see cref="Stream"/>.
        /// </summary>
        /// <typeparam name="T">Type of configuration file.</typeparam>
        /// <param name="stream">The source <see cref="Stream"/>.</param>
        /// <param name="fileName">File name.</param>
        /// <returns>The object representing the configuration file.</returns>
        protected static T Load<T>(Stream stream, string fileName)
        where T : ConfigurationFile {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            XmlDeserializationEvents deserializationEvents = new XmlDeserializationEvents();

            // Currently, let's not handle any deserialization events.
            // I guess the best approach is to log them and apply defaults.
            // TODO:

            //deserializationEvents.OnUnknownAttribute = delegate(object sender, XmlAttributeEventArgs e)
            //{
            //    if (e.Attr.LocalName != "type" ||
            //        e.Attr.NamespaceURI !=
            //        "http://www.w3.org/2001/XMLSchema-instance")
            //    {
            //        // Handle this?
            //        // TODO:
            //    }
            //};
            //deserializationEvents.OnUnknownElement =
            //    (sender, e) =>
            //                 ConfigurationLogSources.Configuration.LogWarning(
            //                     "Unrecognized element {0} in file {1}, line {2}, column {3}. Expected elements: '{4}'.",
            //                     e.Element.Name, fileName, e.LineNumber, e.LinePosition,
            //                     e.ExpectedElements);
            //deserializationEvents.OnUnknownNode = delegate(object sender, XmlNodeEventArgs e)
            //{
            //    if (e.NodeType != XmlNodeType.Attribute &&
            //        e.NodeType != XmlNodeType.Element)
            //    {
            //        ConfigurationLogSources.Configuration.LogWarning(
            //            "Unrecognized node {0} '{1}' in file {2}, line {3}, column {4}. ",
            //            e.NodeType, e.Name, fileName, e.LineNumber,
            //            e.LinePosition);
            //    }
            //};

            //ConfigurationLogSources.Configuration.Trace("Deserializing {0}.", fileName);
            T configurationFile;
            try {
                using (XmlTextReader reader = new XmlTextReader(stream)) {
                    configurationFile = (T)serializer.Deserialize(reader, deserializationEvents);
                }
            } catch (XmlException e) {
                string innerMessage = e.InnerException != null ? e.InnerException.Message : "";
                throw new InvalidConfigurationException(
                    string.Format("Cannot load the file {0}: {1} at line {2}, column {3}. {4}", fileName, e.Message,
                                  e.LineNumber, e.LinePosition, innerMessage));
            }
            configurationFile.ConfigurationFilePath = fileName;
            ((IConfigurationElement)configurationFile).SetParent(null, "");
            return configurationFile;
        }


        /// <inheritdoc />
        public override string GetKeyPath() {
            return string.IsNullOrEmpty(this.configurationFilePath)
                   ? ""
                   : "[" + Path.GetFileName(this.configurationFilePath) + "]";
        }
    }
}