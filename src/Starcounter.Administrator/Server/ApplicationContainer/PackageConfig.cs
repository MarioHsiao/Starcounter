using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Administrator.Server.ApplicationContainer {
    /// <summary>
    /// 
    /// </summary>
    public class PackageConfig {
        public string Namespace;        // Unique appstore namespace
        public string Channel;          // Canary,Stable,Beta,etc..
        public string Version;          // Valid version number

        public string Executable;       // Executable name (relative path)       
        public string ResourceFolder;   // Resource folder
        public string RelativeStartUri; // Application start uri 

        public string DisplayName;      // Application name
        public string Company;          // Company
        public string Description;      // Application Description
        public string ImageUri;         // Application image
        public DateTime VersionDate;
    }
}
