using Starcounter.Internal;
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
        internal const string appConfigurationFileExtention = ".app.config";

        /// <summary>
        /// Install zipped package stream
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="packageZip">Zipped package stream</param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="replace">Replace existing app if any</param>
        /// <param name="config"></param>
        public static void Install(Stream packageZip, string sourceUrl, string appRootFolder, string imageResourceFolder, out AppConfig config) {
            lock (AppsContainer.locker) {

                if (packageZip == null) throw new ArgumentNullException("packageZip");
                if (sourceUrl == null) throw new ArgumentNullException("sourceUrl");
                if (appRootFolder == null) throw new ArgumentNullException("appRootFolder");

                string createdDestinationFolder = null;
                try {

                    using (ZipArchive archive = new ZipArchive(packageZip, ZipArchiveMode.Read)) {

                        // Get Configuration
                        PackageConfig packageConfig;
                        ReadConfiguration(archive, out packageConfig);

                        // Validate configuration
                        ValidateConfiguration(archive, packageConfig);

                        // Prepare to extract package
                        string destinationFolder = Path.Combine(appRootFolder, packageConfig.Namespace);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Channel);
                        destinationFolder = Path.Combine(destinationFolder, packageConfig.Version);

                        try {

                            if (Directory.Exists(destinationFolder)) {
                                throw new InvalidOperationException("Application already installed.", new InvalidOperationException(string.Format("Destination folder exists, {0}", destinationFolder)));
                            }

                            createdDestinationFolder = Package.CreateDirectory(destinationFolder);

                            // Extract package
                            archive.ExtractToDirectory(destinationFolder);

                            string imageUri = string.Empty;

                            // Unpack app image to starcounter admin folder
                            UnpackAppImage(archive, packageConfig, imageResourceFolder, out imageUri);

                            // Create app configuration file
                            config = new AppConfig();

                            CreateAppConfig(packageConfig, out config);

                            Uri u = new Uri(sourceUrl);
                            if (u.IsFile) {
                                config.SourceID = string.Format("{0:X8}", u.LocalPath.GetHashCode());
                            }
                            else {
                                config.SourceID = u.Segments[u.Segments.Length - 1];
                            }

                            config.SourceUrl = sourceUrl;
                            config.ImageUri = imageUri;

                            // Save Application configuration
                            string configFile = Path.Combine(destinationFolder, config.Namespace + Package.appConfigurationFileExtention);
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
        public static void Install(string packageZip, string appRootFolder, string imageResourceFolder, out AppConfig config) {
            lock (AppsContainer.locker) {
                if (!File.Exists(packageZip)) {
                    throw new FileNotFoundException("Package not found", packageZip);
                }

                string sourceHost = new Uri(System.IO.Path.GetFullPath(packageZip)).AbsoluteUri;

                using (FileStream fs = new FileStream(packageZip, FileMode.Open)) {
                    Install(fs, sourceHost, appRootFolder, imageResourceFolder, out config);
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
        private static void UnpackAppImage(ZipArchive archive, PackageConfig config, string imageResourceFolder, out string imageUri) {

            string createdDestinationFolder = Package.CreateDirectory(imageResourceFolder);

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
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="sourceHost"></param>
        /// <param name="packageZip"></param>
        /// <param name="appRootFolder"></param>
        /// <param name="imageResourceFolder">Folder where app image will be created</param>
        /// <param name="config"></param>
        public static void Upgrade(AppConfig from, string packageZip, string appRootFolder, string imageResourceFolder, out AppConfig config) {

            lock (AppsContainer.locker) {
                Package.Install(packageZip, appRootFolder, imageResourceFolder, out config);
                AppsContainer.UnInstall(from, imageResourceFolder);
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
        public static void Upgrade(AppConfig from, string sourceHost, Stream packageZip, string appRootFolder, string imageResourceFolder, out AppConfig config) {

            lock (AppsContainer.locker) {
                Package.Install(packageZip, sourceHost, appRootFolder, imageResourceFolder, out config);
                AppsContainer.UnInstall(from, imageResourceFolder);
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
            appConfig.Namespace = packageConfig.Namespace;
            appConfig.Channel = packageConfig.Channel;
            appConfig.Version = packageConfig.Version;

            appConfig.AppName = packageConfig.AppName;

            appConfig.Executable = packageConfig.Executable;
            appConfig.ResourceFolder = packageConfig.ResourceFolder;

            appConfig.DisplayName = packageConfig.DisplayName;
            appConfig.Company = packageConfig.Company;
            appConfig.Description = packageConfig.Description;
            //appConfig.ImageUri = packageConfig.ImageUri;
            appConfig.VersionDate = packageConfig.VersionDate;
        }

        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(ZipArchive archive, out PackageConfig config) {
            lock (AppsContainer.locker) {

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
                catch (InvalidOperationException e) {
                    throw new InvalidDataException("Invalid package format", e);
                }
                catch (Exception e) {
                    throw e;
                }

                throw new FileNotFoundException(string.Format("Missing package configuration file ({0})", Package.packageConfigurationFileName));
            }
        }

        /// <summary>
        /// Get App configuration from zipped package
        /// </summary>
        /// <param name="packageZip"></param>
        /// <param name="config"></param>
        public static void ReadConfiguration(string packageZip, out PackageConfig config) {
            lock (AppsContainer.locker) {

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
                    PackageConfig packageConfig;
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

        /// <summary>
        /// Create Directory structure 
        /// TODO: Move to Utils
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Created root base folder</returns>
        public static string CreateDirectory(string path) {
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
