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
//        public string ID;               // (Hashed) Name+Channel+Version
        [XmlIgnoreAttribute]
        public string ID {
            get {
                //return string.Format("{0:X8}", (this.Namespace + this.Channel + this.VersionDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'")).GetHashCode());
                return string.Format("{0:X8}", this.File.GetHashCode());
            }
        }
        public string Namespace;        // Unique appstore namespace, used in the apps folder path
        public string Channel;          // Canary,Stable,Beta,etc..
        public string Version;          // Valid version number
        public DateTime VersionDate;    // Version date

        public string AppName;          // AppName (name of the app running in starcounter, can be the bindingname in polyjuice)

        public string Executable;       // Executable name (relative path)
        public string ResourceFolder;   // Resource folder

        public string DisplayName;      // Displayname
        public string Company;          // Company name

        public string Heading;          // Heading
        public string Description;      // Description
        public string ImageUri;         // Image

        public string SourceID;         // Package source ID, "EB23432"
        public string SourceUrl;        // Package source Url, "http://appstore.polyjuice.com/apps/EB23432"

        [XmlIgnoreAttribute]
        public string File;

        /// <summary>
        /// Save 
        /// </summary>
        /// <param name="file"></param>
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
                this.File = file;
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
