using Administrator.Server.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.Model {
    /// <summary>
    /// TODO: Rename class AppConfig to DeployedItem
    /// </summary>
    [XmlRoot(ElementName = "AppConfig")]    // Handle backward compability
    public class DeployedConfigFile {

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
        public string ResourceFolder;   // Resource folder (relative path)

        public string DisplayName;      // Displayname
        public string Company;          // Company name

        public string Heading;          // Heading
        public string Description;      // Description
        public string ImageUri;         // Image

        public string SourceID;         // Package source ID, "EB23432"
        public string SourceUrl;        // Package source Url, "http://appstore.polyjuice.com/apps/EB23432"

        public bool CanBeUninstalled;   // True if the application can be uninstalled

        [XmlIgnoreAttribute]
        public string File;

        public void Save() {

            if (string.IsNullOrEmpty(this.File)) {
                throw new InvalidOperationException("Can not save configuration file without a filename");
            }

            this.Save(this.File);
        }

        /// <summary>
        /// Save 
        /// </summary>
        /// <param name="file"></param>
        public void Save(string file) {

            string createdBaseFolder = null;

            // Assure folder path
            string folder = Path.GetDirectoryName(file);
            if (!Directory.Exists(folder)) {
                createdBaseFolder = Administrator.Server.Utilities.Utils.CreateDirectory(folder);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appsFolder"></param>
        /// <returns></returns>
        public string GetExecutableFullPath(string appsFolder) {

            string executable = appsFolder;
            executable = Path.Combine(executable, this.Namespace);
            executable = Path.Combine(executable, this.Channel);
            executable = Path.Combine(executable, this.Version);
            executable = Path.Combine(executable, this.Executable);
            return Path.GetFullPath(executable);  // Fixes the DirectorySeparatorChar

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appsFolder"></param>
        /// <returns></returns>
        public string GetResourceFullPath(string appsFolder) {

            string resourceFolder = appsFolder;
            resourceFolder = Path.Combine(resourceFolder, this.Namespace);
            resourceFolder = Path.Combine(resourceFolder, this.Channel);
            resourceFolder = Path.Combine(resourceFolder, this.Version);
            resourceFolder = Path.Combine(resourceFolder, this.ResourceFolder);
            resourceFolder = resourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFullPath(resourceFolder);  // Fixes the DirectorySeparatorChar

        }

        /// <summary>
        /// 
        /// </summary>
        public void Verify() {

            DirectoryInfo applicationFolder = new DirectoryInfo(this.File);
            DirectoryInfo versionFolder = applicationFolder.Parent;
            if (string.Compare(versionFolder.Name, this.Version, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, version folder");
            }

            DirectoryInfo channelFolder = versionFolder.Parent;
            if (string.Compare(channelFolder.Name, this.Channel, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, channel folder");
            }

            DirectoryInfo nameSpaceFolder = channelFolder.Parent;
            if (string.Compare(nameSpaceFolder.Name, this.Namespace, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, namespace folder");
            }
        }

        /// <summary>
        /// Read App Config
        /// </summary>
        /// <param name="file"></param>
        /// <param name="config"></param>
        public static void ReadConfig(string file, out DeployedConfigFile config) {

            using (FileStream fs = new FileStream(file, FileMode.Open)) {
                ReadConfig(fs, out config);
                config.File = file;
            }
        }

        /// <summary>
        /// Read App config
        /// </summary>
        /// <param name="s"></param>
        /// <param name="config"></param>
        public static void ReadConfig(Stream s, out DeployedConfigFile config) {

            XmlSerializer ser = new XmlSerializer(typeof(DeployedConfigFile));
            config = ser.Deserialize(s) as DeployedConfigFile;
        }
    }
}
