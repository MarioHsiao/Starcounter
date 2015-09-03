using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.Model {
    /// <summary>
    /// 
    /// </summary>
    [XmlRoot(ElementName = "PackageConfig")]    // Handle backward compability
    public class PackageConfigFile {
        public string Namespace;        // Unique appstore namespace, used in the folder path and for generating the Application ID. 
        public string Channel;          // Channel Name, used in the folder path and for generating the Application ID.
        public string Version;          // Valid version number, used in the folder path and for generating the Application ID.

        public string Executable;       // Executable name (relative path)
        public string ResourceFolder;   // Resource folder

        public string AppName;          // AppName (name of the app running in starcounter)

        public string DisplayName;      // Displayname
        public string Company;          // Company name
        public string Description;      // Description
        public string Heading;          // Heading
        public string ImageUri;         // Image
        public DateTime VersionDate;    // Version date
    }
}
