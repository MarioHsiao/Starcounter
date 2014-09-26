using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.ApplicationContainer {
    public class AppsContainer {

        // Apps/
        //      NameSpace/                              // Myapp1
        //           Channel/                           // Stable
        //                   Version/                   // 2.0.0.0
        //                           *.app.config
        //                           package.config
        //

        /// <summary>
        /// Uninstall installed application
        /// </summary>
        /// <param name="config"></param>
        public static void UnInstall(AppConfig config) {

            if (string.IsNullOrEmpty(config.File)) {
                throw new InvalidOperationException(string.Format("Failed to uninstall application, Missing folder settings."));
            }

            string folder = Path.GetDirectoryName(config.File);
            if (!Directory.Exists(folder)) {
                throw new InvalidOperationException(string.Format("Failed to uninstall application, invalid folder path {0}", folder));
            }

            VerifyAppconfig(config);

            DirectoryInfo di = new DirectoryInfo(folder);
            di.Delete(true); // Remove version folder.

            // Clean up empty folders
            DirectoryInfo channelFolder = di.Parent;
            if (IsDirectoryEmpty(channelFolder.FullName)) {
                channelFolder.Delete();

                DirectoryInfo nameSpaceFolder = di.Parent.Parent;
                if (IsDirectoryEmpty(nameSpaceFolder.FullName)) {
                    nameSpaceFolder.Delete();
                }
            }
        }

        /// <summary>
        /// Check if directory is empty
        /// TODO: Move to Utils
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private static bool IsDirectoryEmpty(string folder) {
            return !Directory.EnumerateFileSystemEntries(folder).Any();
        }

        /// <summary>
        /// Get application base folder
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static void VerifyAppconfig(AppConfig config) {

            DirectoryInfo applicationFolder = new DirectoryInfo(config.File);
            DirectoryInfo versionFolder = applicationFolder.Parent;
            if (string.Compare(versionFolder.Name, config.Version, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, version folder");
            }

            DirectoryInfo channelFolder = versionFolder.Parent;
            if (string.Compare(channelFolder.Name, config.Channel, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, channel folder");
            }

            DirectoryInfo nameSpaceFolder = channelFolder.Parent;
            if (string.Compare(nameSpaceFolder.Name, config.Namespace, true) != 0) {
                throw new InvalidOperationException("Invalid configuration content missmatch, namespace folder");
            }
        }


        /// <summary>
        /// Get a list of installed apps configurations
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IList<AppConfig> GetInstallApps(string path) {

            IList<AppConfig> appConfigs = new List<AppConfig>();

            IList<string> configs = GetAppConfigs(path);

            foreach (string file in configs) {
                AppConfig config;
                try {
                    AppsContainer.ReadConfig(file, out config);
                    appConfigs.Add(config);
                }
                catch (Exception e) {
                    throw new Exception(string.Format("Invalid application configuration file {0}, {1}", file, e.Message));
                }
            }

            return appConfigs;
        }

        /// <summary>
        /// Get Installed application configuration files
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IList<string> GetAppConfigs(string path) {

            List<string> configs = new List<string>();

            if (!Directory.Exists(path)) {
                return configs;
            }

            foreach (string file in Directory.EnumerateFiles(path, "*.app.config", SearchOption.AllDirectories)) {
                configs.Add(file);
            }
            return configs;
        }

        /// <summary>
        /// Read App Config
        /// </summary>
        /// <param name="file"></param>
        /// <param name="config"></param>
        private static void ReadConfig(string file, out AppConfig config) {

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
        private static void ReadConfig(Stream s, out AppConfig config) {

            XmlSerializer ser = new XmlSerializer(typeof(AppConfig));
            config = ser.Deserialize(s) as AppConfig;
        }
    }
}
