using System;
using System.IO;
using System.Xml.Serialization;

namespace Starcounter.Apps.Package.Config {
    /// <summary>
    /// 
    /// </summary>
    [XmlRoot(ElementName = "PackageConfig")]    // Handle backward compability
    public class PackageConfigFile : IConfig {
        public string Namespace;        // Unique appstore namespace, used in the folder path and for generating the Application ID. 
        public string Channel { get; set; }          // Channel Name, used in the folder path and for generating the Application ID.
        public string Version { get; set; }          // Valid version number, used in the folder path and for generating the Application ID.

        public string Executable;       // Executable name (relative path)
        public string ResourceFolder;   // Resource folder

        public string AppName;          // AppName (name of the app running in starcounter)

        public string DisplayName;      // Displayname
        public string Company;          // Company name
        public string Description;      // Description
        public string Heading;          // Heading
        public string ImageUri;         // Image
        public DateTime VersionDate;    // Version date
        [XmlElement("Dependency")]
        public Dependency[] Dependencies { get; set; }

        [XmlIgnoreAttribute]
        public string ID {
            get {
                return Namespace;
            }
            set {
                Namespace = value;
            }
        }

        public string GetString() {

            XmlSerializer ser = new XmlSerializer(this.GetType());
            using (MemoryStream ms = new MemoryStream()) {
                ser.Serialize(ms, this);
                ms.Seek(0, SeekOrigin.Begin);

                var sr = new StreamReader(ms);
                return sr.ReadToEnd();

            }
        }

        internal static IConfig Deserialize(Stream stream) {

            PackageConfigFile config = new PackageConfigFile();
            XmlSerializer ser = new XmlSerializer(config.GetType());
            return ser.Deserialize(stream) as IConfig;
        }
    }

    public class Dependency {
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlText]
        public string Value;
    }
}
