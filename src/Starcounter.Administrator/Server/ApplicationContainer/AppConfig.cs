using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.ApplicationContainer {
    /// <summary>
    /// 
    /// </summary>
    public class AppConfig {
        public string ID;               // (Hashed) Name+Channel+Version
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
        public string SourceUrl;        // Package source
        public DateTime VersionDate;

        [XmlIgnoreAttribute]
        public string File;

        public void Save(string file) {

            string createdBaseFolder = null;

            // Assure folder path
            string folder = Path.GetDirectoryName(file);
            if (!Directory.Exists(folder)) {
                createdBaseFolder = Package.CreateDirectory(folder);
            }

            try {
                XmlSerializer ser = new XmlSerializer(this.GetType());

                using (TextWriter txtWriter = new StreamWriter(file)) {
                    ser.Serialize(txtWriter, this);
                    //txtWriter.Close();
                }


            }
            catch (Exception e) {

                if (createdBaseFolder != null) {
                    Directory.Delete(createdBaseFolder, true);
                }
                throw e;
            }
        }
    }
}
