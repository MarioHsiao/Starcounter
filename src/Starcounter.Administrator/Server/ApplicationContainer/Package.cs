using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Administrator.Server.ApplicationContainer {
    public class Package {

        const string packageConfigurationFileName = "package.config";
        const string appConfigurationFileExtention = ".app.config";

        /// <summary>
        /// Install zipped package stream
        /// </summary>
        /// <param name="packageZip">Zipped package stream</param>
        /// <param name="appRootFolder"></param>
        public static void Install(string sourceUrl, Stream packageZip, string appRootFolder, bool update) {

            if (packageZip == null) throw new ArgumentNullException("packageZip");
            if (appRootFolder == null) throw new ArgumentNullException("appRootFolder");

            string createdDestinationFolder = null;
            try {

                using (ZipArchive archive = new ZipArchive(packageZip, ZipArchiveMode.Read)) {

                    try {
                        // Get Configuration
                        PackageConfig packageConfig;
                        ReadConfiguration(archive, out packageConfig);

                        // Validate configuration
                        ValidateConfiguration(archive, packageConfig);

                        // Prepare to extract package
                        string destinationFolder = Path.Combine(appRootFolder, packageConfig.Namespace);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Channel);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Version);

                        if (Directory.Exists(destinationFolder)) {

                            if (update) {
                                // TODO: Check version etc..
                                throw new NotImplementedException("Updating packages is not yet available, Use uninstall and reinstall to update an application");
                            }
                            else {
                                throw new InvalidOperationException("Application already exists");
                            }

                        }
                        else {
                            try {
                                createdDestinationFolder = Package.CreateDirectory(destinationFolder);
                            }
                            catch (ArgumentException) {
                                throw new ArgumentException("Invalid applications folder path");
                            }
                        }

                        // Extract pagage
                        archive.ExtractToDirectory(destinationFolder);

                        // Create app configuration file
                        AppConfig appConfig = new AppConfig();
                        CreateAppConfig(packageConfig, out appConfig);
                        appConfig.SourceUrl = sourceUrl;

                        // Save Application configuration
                        string configFile = Path.Combine(destinationFolder, appConfig.Namespace + Package.appConfigurationFileExtention);
                        appConfig.Save(configFile);
                    }
                    catch (Exception e) {

                        if (createdDestinationFolder != null) {
                            Directory.Delete(createdDestinationFolder, true);
                        }
                        throw e;
                    }
                }
            }
            catch (InvalidDataException) {
                throw new Exception("Failed to install package, Invalid package format");
            }
            catch (InvalidOperationException e) {
                // Zip file error
                throw new InvalidOperationException("Failed to install package, " + e.Message);
            }
            catch (Exception e) {
                // Zip file error
                throw new Exception("Failed to install package, " + e.Message);
            }
        }

        /// <summary>
        /// Install zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="appRootFolder"></param>
        public static void Install(string sourceHost, string packageZip, string appRootFolder, bool update) {

            if (!File.Exists(packageZip)) {
                throw new FileNotFoundException("Package not found", packageZip);
            }

            using (FileStream fs = new FileStream(packageZip, FileMode.Open)) {
                Install(sourceHost, fs, appRootFolder, update);
            }
        }

        /// <summary>
        /// Create App configuration from package configuration
        /// </summary>
        /// <param name="packageConfig"></param>
        /// <param name="appConfig"></param>
        private static void CreateAppConfig(PackageConfig packageConfig, out AppConfig appConfig) {

            // Create app configuration file
            appConfig = new AppConfig();
            appConfig.ID = string.Format("{0:X8}", (packageConfig.Namespace + packageConfig.Channel + packageConfig.Version).GetHashCode());
            appConfig.Namespace = packageConfig.Namespace;
            appConfig.Channel = packageConfig.Channel;
            appConfig.Version = packageConfig.Version;

            appConfig.Executable = packageConfig.Executable;
            appConfig.ResourceFolder = packageConfig.ResourceFolder;

            appConfig.DisplayName = packageConfig.DisplayName;
            appConfig.Company = packageConfig.Company;
            appConfig.Description = packageConfig.Description;
            appConfig.ImageUri = packageConfig.ImageUri;
            appConfig.VersionDate = packageConfig.VersionDate;
            appConfig.RelativeStartUri = packageConfig.RelativeStartUri;
        }

        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(ZipArchive archive, out PackageConfig config) {

            try {

                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.FullName.Equals(Package.packageConfigurationFileName, StringComparison.OrdinalIgnoreCase)) {

                        Stream s = entry.Open();
                        XmlSerializer ser = new XmlSerializer(typeof(PackageConfig));
                        config = ser.Deserialize(s) as PackageConfig;
                        return;
                    }
                }
            }
            catch (InvalidOperationException) {
                throw new Exception("Invalid package format");
            }
            catch (Exception e) {
                throw e;
            }

            throw new FileNotFoundException(string.Format("Missing package configuration file ({0})", Package.packageConfigurationFileName));
        }

        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(string packageZip, out PackageConfig config) {

            using (ZipArchive archive = ZipFile.OpenRead(packageZip)) {
                ReadConfiguration(archive, out config);
            }
        }

        /// <summary>
        /// Validate app configuration
        /// </summary>
        /// <param name="config"></param>
        private static void ValidateConfiguration(ZipArchive archive, PackageConfig config) {

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

            try {
                new Version(config.Version);
            }
            catch (Exception) {
                throw new InvalidOperationException("Invalid Version <tag> in package configuration");
            }

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
            // Note: Resource folder can be empty
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
            catch (InvalidOperationException) {
                throw new Exception("Invalid package the configuration format");
            }
            catch (Exception e) {
                throw e;
            }

            if (bExist == false) {
                throw new FileNotFoundException(string.Format("Invalid or missing package entry"));
            }
        }

        /// <summary>
        /// 
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
            catch (InvalidOperationException) {
                throw new Exception("Invalid package format");
            }
            catch (Exception e) {
                throw e;
            }

            throw new FileNotFoundException("Could not find the file");
        }


        /// <summary>
        /// Create Directory structure 
        /// TODO: Move to Utils
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Created root base folder</returns>
        static public string CreateDirectory(string path) {
            string createdBaseFolder = null;

            DirectoryInfo di = new DirectoryInfo(path);

            while (di.Exists == false) {
                createdBaseFolder = di.FullName;
                di = di.Parent;
            }

            Directory.CreateDirectory(path);

            return createdBaseFolder;
        }

        /// <summary>
        /// TODO: Create zipped package 
        /// TODO: based on a .csproj file?
        /// </summary>
        /// <param name="config"></param>
        /// <param name="packageZip"></param>
        public static void CreatePackage(PackageConfig config, string packageZip) {

            // TODO:
        }
    }
}
