using Administrator.Server.Model;
using Starcounter;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.Managers {

    /// <summary>
    /// Responsable for:
    /// * Unpack (Deploy and unpacking packages)
    /// * Delete deployed packages
    /// </summary>
    public class PackageManager {

        internal static readonly object locker = new object();

        const string packageConfigurationFileName = "package.config";
        internal const string deployedConfigurationFileExtention = ".app.config";

        /// <summary>
        /// Unpack zipped package stream
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="packageZip">Zipped package stream</param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="replace">Replace existing app if any</param>
        /// <param name="config"></param>
        public static void Unpack(Stream packageZip, string sourceUrl, string storeUrl, string appRootFolder, string imageResourceFolder, out DeployedConfigFile config) {

            lock (PackageManager.locker) {

                if (packageZip == null) throw new ArgumentNullException("packageZip");
                if (sourceUrl == null) throw new ArgumentNullException("sourceUrl");
                if (storeUrl == null) throw new ArgumentNullException("storeUrl");
                if (appRootFolder == null) throw new ArgumentNullException("appRootFolder");

                string createdDestinationFolder = null;
                try {

                    using (ZipArchive archive = new ZipArchive(packageZip, ZipArchiveMode.Read)) {

                        // Get Configuration
                        PackageConfigFile packageConfig;
                        ReadConfiguration(archive, out packageConfig);

                        // Validate configuration
                        ValidateConfiguration(archive, packageConfig);

                        // Prepare to extract package
                        string destinationFolder = Path.Combine(appRootFolder, packageConfig.Namespace);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Channel);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Version);

                        try {

                            if (Directory.Exists(destinationFolder)) {
                                config = GetInstalledApp(destinationFolder);
                                throw new InvalidOperationException("Application already installed.", new InvalidOperationException(string.Format("Destination folder exists, {0}", destinationFolder)));
                            }

                            createdDestinationFolder = Administrator.Server.Utilities.Utils.CreateDirectory(destinationFolder);

                            // Extract package
                            archive.ExtractToDirectory(destinationFolder);

                            string imageUri = string.Empty;

                            // Unpack app image to starcounter admin folder
                            UnpackAppImage(archive, packageConfig, imageResourceFolder, out imageUri);

                            // Create app configuration file
                            //config = new DeployedConfigFile();
                            CreateConfig(packageConfig, sourceUrl, storeUrl, imageUri, out config);

                            //Uri u = new Uri(sourceUrl);
                            //if (u.IsFile) {
                            //    config.SourceID = string.Format("{0:X8}", u.LocalPath.GetHashCode());
                            //}
                            //else {
                            //    config.SourceID = u.Segments[u.Segments.Length - 1];
                            //}

                            //config.SourceUrl = sourceUrl;
                            //config.ImageUri = imageUri;

                            // Save Application configuration
                            string configFile = Path.Combine(destinationFolder, config.Namespace + PackageManager.deployedConfigurationFileExtention);
                            config.Save(configFile);
                        }
                        catch (Exception e) {

                            if (createdDestinationFolder != null) {
                                Directory.Delete(createdDestinationFolder, true);
                            }
                            throw e;
                        }
                    }
                }
                catch (InvalidDataException e) {
                    throw new InvalidOperationException("Failed to install package, Invalid package format", e);
                }
                catch (Exception e) {

                    throw new InvalidOperationException("Failed to install package, " + e.Message, e);
                }
            }
        }


        /// <summary>
        /// Install zipped package
        /// </summary>
        /// <param name="packageZip">Zipped package</param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="config"></param>
        public static void Unpack(string packageZip, string appRootFolder, string imageResourceFolder, out DeployedConfigFile config) {

            lock (PackageManager.locker) {

                if (!File.Exists(packageZip)) {
                    throw new FileNotFoundException("Package not found", packageZip);
                }

                string sourceHost = new Uri(System.IO.Path.GetFullPath(packageZip)).AbsoluteUri;
                string storeUrl = string.Empty;

                using (FileStream fs = new FileStream(packageZip, FileMode.Open)) {
                    Unpack(fs, sourceHost, storeUrl, appRootFolder, imageResourceFolder, out config);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="sourceHost"></param>
        /// <param name="packageZip"></param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="config"></param>
        //public static void Upgrade(DeployedConfigFile from, string packageZip, string appRootFolder, string imageResourceFolder, out DeployedConfigFile config) {

        //    lock (PackageManager.locker) {
        //        PackageManager.Unpack(packageZip, appRootFolder, imageResourceFolder, out config);
        //        PackageManager.Delete(from, imageResourceFolder);
        //    }
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="sourceHost"></param>
        /// <param name="packageZip"></param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="config"></param>
        //public static void Upgrade(DeployedConfigFile from, string sourceHost, Stream packageZip, string appRootFolder, string imageResourceFolder, out DeployedConfigFile config) {

        //    lock (PackageManager.locker) {
        //        PackageManager.Unpack(packageZip, sourceHost, appRootFolder, imageResourceFolder, out config);
        //        PackageManager.Delete(from, imageResourceFolder);
        //    }
        //}

        /// <summary>
        /// Delete unpacked package
        /// </summary>
        /// <param name="config"></param>
        public static void Delete(DeployedConfigFile config, string imageResourceFolder) {

            lock (PackageManager.locker) {

                if (string.IsNullOrEmpty(config.File)) {
                    throw new InvalidOperationException(string.Format("Failed to uninstall application, Missing folder settings."));
                }

                string folder = Path.GetDirectoryName(config.File);
                if (!Directory.Exists(folder)) {
                    throw new InvalidOperationException(string.Format("Failed to uninstall application, invalid folder path {0}", folder));
                }

                config.Verify();

                DirectoryInfo di = new DirectoryInfo(folder);
                di.Delete(true); // Remove version folder.

                // Delete app image
                string imageFile = Path.Combine(imageResourceFolder, config.ImageUri);
                if (File.Exists(imageFile)) {
                    File.Delete(imageFile);
                }

                // Clean up empty folders
                DirectoryInfo channelFolder = di.Parent;
                if (Administrator.Server.Utilities.Utils.IsDirectoryEmpty(channelFolder.FullName)) {
                    channelFolder.Delete();

                    DirectoryInfo nameSpaceFolder = di.Parent.Parent;
                    if (Administrator.Server.Utilities.Utils.IsDirectoryEmpty(nameSpaceFolder.FullName)) {
                        nameSpaceFolder.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="config"></param>
        /// <param name="imageResourceFolder"></param>
        /// <param name="imageUri"></param>
        private static void UnpackAppImage(ZipArchive archive, PackageConfigFile config, string imageResourceFolder, out string imageUri) {

            string createdDestinationFolder = Administrator.Server.Utilities.Utils.CreateDirectory(imageResourceFolder);

            try {
                // Unpack app image to starcounter admin folder
                ZipArchiveEntry entry = archive.GetEntry(config.ImageUri);
                if (entry != null) {

                    string imageFileName = Path.ChangeExtension(Path.GetRandomFileName(), Path.GetExtension(config.ImageUri));
                    string imageFile = Path.Combine(imageResourceFolder, imageFileName);
                    entry.ExtractToFile(imageFile, true);

                    string imageFolder = Path.GetFileName(imageResourceFolder);
                    imageUri = string.Format("{0}", imageFileName);
                }
                else {
                    // TODO: Use default image?
                    imageUri = config.ImageUri;
                }
            }
            catch (Exception e) {

                if (createdDestinationFolder != null) {
                    Directory.Delete(createdDestinationFolder, true);
                }
                throw e;
            }
        }

        /// <summary>
        /// Create App configuration from package configuration
        /// </summary>
        /// <param name="packageConfig"></param>
        /// <param name="config"></param>
        private static void CreateConfig(PackageConfigFile packageConfig, string sourceUrl, string storeUrl, string imageUri, out DeployedConfigFile config) {

            // Create app configuration file
            config = new DeployedConfigFile();
            config.Namespace = packageConfig.Namespace;
            config.Channel = packageConfig.Channel;
            config.Version = packageConfig.Version;

            config.AppName = packageConfig.AppName;

            config.Executable = packageConfig.Executable;
            config.ResourceFolder = packageConfig.ResourceFolder;

            config.DisplayName = packageConfig.DisplayName;
            config.Company = packageConfig.Company;
            config.Description = packageConfig.Description;
            config.Heading = packageConfig.Heading;
            config.VersionDate = packageConfig.VersionDate;

            Uri u = new Uri(sourceUrl);
            if (u.IsFile) {
                config.SourceID = string.Format("{0:X8}", u.LocalPath.GetHashCode());
            }
            else {
                config.SourceID = u.Segments[u.Segments.Length - 1];
            }
            config.SourceUrl = sourceUrl;

            Uri su = new Uri(storeUrl);
            if (su.IsFile) {
                config.StoreID = string.Format("{0:X8}", su.LocalPath.GetHashCode());
            }
            else {
                config.StoreID = su.Segments[su.Segments.Length - 1];
            }
            config.StoreUrl = storeUrl;

            config.ImageUri = imageUri;
            config.CanBeUninstalled = true;
        }


        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(ZipArchive archive, out PackageConfigFile config) {
            lock (PackageManager.locker) {

                try {

                    foreach (ZipArchiveEntry entry in archive.Entries) {
                        if (entry.FullName.Equals(PackageManager.packageConfigurationFileName, StringComparison.OrdinalIgnoreCase)) {

                            Stream s = entry.Open();
                            XmlSerializer ser = new XmlSerializer(typeof(PackageConfigFile));
                            config = ser.Deserialize(s) as PackageConfigFile;
                            return;
                        }
                    }
                }
                catch (InvalidOperationException e) {
                    throw new InvalidDataException("Invalid package format", e);
                }
                catch (Exception e) {
                    throw e;
                }

                throw new FileNotFoundException(string.Format("Missing package configuration file ({0})", PackageManager.packageConfigurationFileName));
            }
        }

        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(string packageZip, out PackageConfigFile config) {
            lock (PackageManager.locker) {

                using (ZipArchive archive = ZipFile.OpenRead(packageZip)) {
                    ReadConfiguration(archive, out config);
                }
            }
        }

        public static void VerifyPacket(string packageZip) {

            using (FileStream fs = new FileStream(packageZip, FileMode.Open)) {
                VerifyPacket(fs);
            }
        }

        public static void VerifyPacket(Stream package) {

            try {

                using (ZipArchive archive = new ZipArchive(package, ZipArchiveMode.Read, true)) {

                    // Get Configuration
                    PackageConfigFile packageConfig;
                    ReadConfiguration(archive, out packageConfig);

                    // Validate configuration
                    ValidateConfiguration(archive, packageConfig);

                    if (package.CanSeek) {
                        package.Seek(0, 0);
                    }
                };
            }
            catch (InvalidDataException e) {
                throw new InvalidOperationException("Verification of package failed", e);
            }
            catch (Exception e) {
                throw new InvalidOperationException("Verification of package failed, " + e.Message, e);
            }
        }

        /// <summary>
        /// Validate app configuration
        /// </summary>
        /// <param name="config"></param>
        private static void ValidateConfiguration(ZipArchive archive, PackageConfigFile config) {

            if (string.IsNullOrEmpty(config.Namespace)) {
                throw new InvalidOperationException("Invalid Namespace <tag> in package configuration");
            }

            // TODO: Validate Namespace for invalid characters (this will be uses for folder naming)

            if (string.IsNullOrEmpty(config.DisplayName)) {
                throw new InvalidOperationException("Invalid DisplayName <tag> in package configuration");
            }

            if (string.IsNullOrEmpty(config.Channel)) {
                throw new InvalidOperationException("Invalid Channel <tag> in package configuration");
            }

            // TODO: Validate Channel for invalid characters (this will be uses for folder naming)


            if (string.IsNullOrEmpty(config.Version)) {
                throw new InvalidOperationException("Invalid Version <tag> in package configuration");
            }

            //try {
            //    new Version(config.Version);
            //}
            //catch (Exception) {
            //    throw new InvalidOperationException("Invalid Version <tag> in package configuration");
            //}

            if (string.IsNullOrEmpty(config.Executable)) {
                throw new InvalidOperationException("Invalid Executable <tag> in package configuration");
            }

            try {
                VerifyPackageEntry(archive, config.Executable);
            }
            catch (Exception e) {
                throw new InvalidOperationException(e.Message + ", " + config.Executable);
            }

            // Resource folder
            // Note: Resource tag can be empty
            if (!string.IsNullOrEmpty(config.ResourceFolder)) {

                // Assure that the path is a folder path (ending with an "/" )
                if (!config.ResourceFolder.EndsWith("/")) {
                    config.ResourceFolder += "/";
                }

                try {
                    VerifyPackageEntry(archive, config.ResourceFolder);
                }
                catch (Exception e) {
                    throw new InvalidOperationException(e.Message + ", " + config.ResourceFolder);
                }

            }

            if (!string.IsNullOrEmpty(config.ImageUri)) {
                try {
                    VerifyPackageEntry(archive, config.ImageUri);
                }
                catch (Exception e) {
                    throw new InvalidOperationException(e.Message + ", " + config.ImageUri);
                }
            }

            // TODO: Add more validations
        }

        /// <summary>
        /// Verify Package entry
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        private static void VerifyPackageEntry(ZipArchive archive, string entryName) {

            bool bExist = false;

            try {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.FullName.Equals(entryName, StringComparison.OrdinalIgnoreCase)) {
                        bExist = true;
                        break;
                    }
                }
            }
            catch (InvalidOperationException e) {
                throw new InvalidDataException("Invalid package format", e);
            }
            catch (Exception e) {
                throw e;
            }

            if (bExist == false) {
                throw new FileNotFoundException(string.Format("Missing package entry, {0}", entryName));
            }
        }

        /// <summary>
        /// Unpack file from an archive
        /// </summary>
        /// <param name="archive"></param>
        /// <param name="file"></param>
        /// <param name="destinationFileName"></param>
        public static void UnpackFile(ZipArchive archive, string file, string destinationFileName) {

            try {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.FullName.Equals(file, StringComparison.OrdinalIgnoreCase)) {

                        entry.ExtractToFile(destinationFileName, true);
                        return;
                    }
                }
            }
            catch (InvalidOperationException e) {
                throw new InvalidDataException("Invalid package format", e);
            }
            catch (Exception e) {
                throw e;
            }

            throw new FileNotFoundException("Failed to find file in package, {0}", file);
        }

        #region AppContainer

        /// <summary>
        /// Get a list of installed apps configurations
        /// TODO: Rename to GetDepolyedApps
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IList<DeployedConfigFile> GetInstallApps(string path) {
            lock (PackageManager.locker) {

                IList<DeployedConfigFile> appConfigs = new List<DeployedConfigFile>();

                IList<string> configs = GetAppConfigs(path, SearchOption.AllDirectories);

                foreach (string file in configs) {
                    DeployedConfigFile config;
                    try {
                        DeployedConfigFile.ReadConfig(file, out config);
                        appConfigs.Add(config);
                    }
                    catch (Exception e) {
                        // TODO: Recreate the config file from the package config file if possible
                        LogSources.Hosting.LogException(e, string.Format("Invalid application configuration file {0}", file));
                    }
                }

                return appConfigs;
            }
        }

        /// <summary>
        /// Get installed App in a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static DeployedConfigFile GetInstalledApp(string path) {

            IList<string> configs = GetAppConfigs(path, SearchOption.TopDirectoryOnly);
            if (configs == null || configs.Count == 0) {
                return null;
            }
            if (configs.Count > 1) {
                throw new InvalidOperationException(string.Format("Multiple App configs is not allowed in one folder, {0}", path));
            }

            DeployedConfigFile config;

            try {
                DeployedConfigFile.ReadConfig(configs[0], out config);
                return config;
            }
            catch (Exception e) {
                throw new Exception(string.Format("Invalid application configuration file {0}, {1}", configs[0], e.Message));
            }

        }

        /// <summary>
        /// Get installed App in a folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static void GetInstalledApp(string id, string path, out DeployedConfigFile installedConfig) {

            installedConfig = null;
            IList<DeployedConfigFile> apps = GetInstallApps(path);

            foreach (DeployedConfigFile config in apps) {
                if (config.ID == id) {
                    installedConfig = config;
                }
            }
        }

        /// <summary>
        /// Get Installed application configuration files
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static IList<string> GetAppConfigs(string path, System.IO.SearchOption searchOptions) {

            List<string> configs = new List<string>();

            if (!Directory.Exists(path)) {
                return configs;
            }

            foreach (string file in Directory.EnumerateFiles(path, "*" + PackageManager.deployedConfigurationFileExtention, searchOptions)) {
                configs.Add(file);
            }
            return configs;
        }

        #endregion
        // TODO:
    }
}
