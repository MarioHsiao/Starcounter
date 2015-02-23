using Starcounter;
using Starcounter.Internal;
using StarcounterApplicationWebSocket.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Starcounter.Applications.UsageTrackerApp.VersionHandler {
    internal class PackageHandler {

        /// <summary>
        /// Unpack file
        /// </summary>
        /// <param name="file">Fullpath to package</param>
        /// <param name="destination">Destination where to unpack</param>
        /// <returns></returns>
        internal static bool Unpack(string file, string destination) {

            // Unpack package
            try {
                LogWriter.WriteLine(string.Format("NOTICE: Unpacking package {0} to {1}.", file, destination));
                ZipFile.ExtractToDirectory(file, destination);
                LogWriter.WriteLine(string.Format("NOTICE: Successfully unpacked package {0} to {1}.", file, destination));
                return true;
            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Failed to unpack package {0} to folder {1}. {2}.", file, destination, e.Message));
                try {
                    // Cleanup destination folder
                    if (Directory.Exists(destination)) {
                        Directory.Delete(destination, true);
                    }
                    return false;
                }
                catch (Exception ee) {
                    LogWriter.WriteLine(string.Format("ERROR: Failed to cleanup destination folder {0}. {1}.", destination, ee.Message));
                    return false;
                }
            }

        }


        /// <summary>
        /// Read packaged information
        /// </summary>
        /// <param name="file"></param>
        /// <param name="version"></param>
        /// <param name="channel"></param>
        /// <param name="edition"></param>
        /// <param name="sourceUTCDate"></param>
        /// <param name="builderTool"></param>
        /// <returns></returns>
        internal static bool ReadPackageInfo(string file, out string edition, out string channel, out string version, out DateTime sourceUTCDate, out string builderTool) {

            edition = string.Empty;
            channel = string.Empty;
            version = string.Empty;
            sourceUTCDate = DateTime.MinValue;
            builderTool = string.Empty;

            using (ZipArchive archive = ZipFile.OpenRead(file)) {
                foreach (ZipArchiveEntry entry in archive.Entries) {

                    if (entry.FullName.Equals(StarcounterEnvironment.FileNames.VersionInfoFileName, StringComparison.OrdinalIgnoreCase)) {
                        Stream s = entry.Open();

                        ReadInfoVersionInfo(s, out edition, out channel, out version, out sourceUTCDate);

                        if (sourceUTCDate == DateTime.MinValue) {
                            sourceUTCDate = entry.LastWriteTime.UtcDateTime;
                            LogWriter.WriteLine(string.Format("WARNING: VersionDate tag is missing from {0} in package {1}. Using the file date as version date.", StarcounterEnvironment.FileNames.VersionInfoFileName, file));
                        }

                        s.Close();
                        s.Dispose();
                    }
                    else if (entry.FullName.Equals("GenerateInstaller.exe", StringComparison.OrdinalIgnoreCase)) {
                        builderTool = "GenerateInstaller.exe";
                    }

                }
            }

            return edition != string.Empty && channel != string.Empty && version != string.Empty && builderTool != string.Empty && sourceUTCDate != DateTime.MinValue;
        }


        /// <summary>
        /// Read out version information
        /// </summary>
        /// <param name="s"></param>
        /// <param name="version"></param>
        /// <param name="channel"></param>
        /// <param name="edition"></param>
        /// <param name="sourceUTCDate"></param>
        internal static void ReadInfoVersionInfo(Stream s, out string edition, out string channel, out string version, out DateTime sourceUTCDate) {

            edition = string.Empty;
            channel = string.Empty;
            version = string.Empty;
            sourceUTCDate = DateTime.MinValue;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(s);

            XmlNodeList versionInfo = xmlDoc.GetElementsByTagName("VersionInfo");
            if (versionInfo != null && versionInfo.Count > 0) {

                XmlNodeList nodeList = versionInfo[0].SelectNodes("Edition");
                if (nodeList != null && nodeList.Count > 0) {
                    edition = nodeList[0].InnerText;
                }

                nodeList = versionInfo[0].SelectNodes("Channel");
                if (nodeList != null && nodeList.Count > 0) {
                    channel = nodeList[0].InnerText;
                }

                nodeList = versionInfo[0].SelectNodes("Version");
                if (nodeList != null && nodeList.Count > 0) {
                    version = nodeList[0].InnerText;
                }

                nodeList = versionInfo[0].SelectNodes("VersionDate");
                if (nodeList != null && nodeList.Count > 0) {
                    DateTime.TryParse(nodeList[0].InnerText, CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out sourceUTCDate);
                }

            }

        }


        /// <summary>
        /// Validate and Add the file entry to database
        /// </summary>
        /// <remarks>This adds a VersionSource instance to the database</remarks>
        /// <param name="file">Zipped package source file</param>
        /// <param name="message"></param>
        /// <returns>True if succesfull otherwise false</returns>
        internal static bool AddFileEntryToDatabase(string file, out string message) {

            message = string.Empty;

            try {

                string edition = string.Empty;
                string channel = string.Empty;
                string version = string.Empty;
                string builderTool = string.Empty;
                DateTime sourceUTCDate = DateTime.MinValue;

                if (!File.Exists(file)) {
                    throw new FileNotFoundException("Can not find the file", file);
                }

                // Validate the file.
                bool result = PackageHandler.ReadPackageInfo(file, out edition, out channel, out version, out sourceUTCDate, out builderTool);

                if (result == false) {
                    message = "Invalid package content.";

                    if (edition == string.Empty) {
                        message += " Missing <Edition> tag information.";
                    }
                    if (channel == string.Empty) {
                        message += " Missing <Channel> tag information.";
                    }
                    if (version == string.Empty) {
                        message += " Missing <Version> tag information.";
                    }
                    if (sourceUTCDate == DateTime.MinValue) {
                        message += " Failed to read the version date from " + StarcounterEnvironment.FileNames.VersionInfoFileName + ".";
                    }
                    if (builderTool == string.Empty) {
                        message += " Missing builder tool GenerateInstaller.exe";
                    }

                    // Invalid package
                    if (File.Exists(file)) {
                        try {
                            File.Delete(file);
                            LogWriter.WriteLine(string.Format("NOTICE: The invalid package {0} was deleted.", file));
                        }
                        catch (Exception e) {
                            LogWriter.WriteLine(string.Format("ERROR: Failed to delete the invalid package {0}. {1}.", file, e.Message));
                        }
                    }
                    return false;
                }

                // Check if version already exist in database
                // Note, The version number can be the same between editions
                VersionSource dupCheckResult = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.Version=?", edition, channel, version).First;
                if (dupCheckResult != null) {
                    message = string.Format("The version {0}-{1}-{2} already exist in the database", edition, channel, version);

                    // Already unpacked
                    if (File.Exists(file)) {
                        try {
                            File.Delete(file);
                            LogWriter.WriteLine(string.Format("NOTICE: Duplicated source, Already unpacked package {0} was deleted.", file));
                        }
                        catch (Exception e) {
                            LogWriter.WriteLine(string.Format("ERROR: Failed to delete the invalid package {0}. {1}.", file, e.Message));
                        }
                    }

                    return false;
                }

                // Save info to database
                Db.Transact(() => {
                    VersionSource versionSource = new VersionSource();
                    versionSource.SourceFolder = null;
                    versionSource.PackageFile = file;
                    versionSource.Edition = edition;
                    versionSource.Channel = channel;
                    versionSource.Version = version;
                    versionSource.BuildError = false;
                    versionSource.VersionDate = sourceUTCDate;
                });

                LogWriter.WriteLine(string.Format("NOTICE: Package {0} was added to database.", file));

                return true;
            }
            catch (Exception e) {

                LogWriter.WriteLine(string.Format("ERROR: Unpacking {0}. {1}.", file, e.Message));

                if (File.Exists(file)) {
                    File.Delete(file);
                    LogWriter.WriteLine(string.Format("NOTICE: Package {0} was delete.", file));
                }
                throw e;
            }

        }


        /// <summary>
        /// Move Source documentation folder to configured destination
        /// </summary>
        /// <param name="versionSource"></param>
        /// <returns>True if documentation folder was moved</returns>
        internal static bool MoveDocumentation(VersionSource versionSource) {

            if (string.IsNullOrEmpty(versionSource.SourceFolder)) {
                LogWriter.WriteLine(string.Format("WARNING: The package {0} has not yet been unpacked.", versionSource.PackageFile));
                return false;
            }

            // Add '\docs\public' to source folder path
            string sourceDocsFolder = Path.Combine(versionSource.SourceFolder, "docs");
            sourceDocsFolder = Path.Combine(sourceDocsFolder, "public");

            // Source folder path example: <usersettings>\oem\nightlybuilds\2.0.904.3

            if (!Directory.Exists(sourceDocsFolder)) {
                LogWriter.WriteLine(string.Format("WARNING: The package {0}-{1}-{2} doesn't contain a documentation folder.", versionSource.Edition, versionSource.Channel, versionSource.Version));
                return false;
            }


            // Destination documentation path

            string outputDocumentationFolder = GetDocumentationFolder(versionSource.Edition, versionSource.Channel);

            if (outputDocumentationFolder == null) {
                return false;
            }


            // Create directory Without the final folder otherwise Directory.Move() will complain.
            if (!Directory.Exists(outputDocumentationFolder)) {
                Directory.CreateDirectory(outputDocumentationFolder);
            }

            outputDocumentationFolder = Path.Combine(outputDocumentationFolder, versionSource.Version);
            // Example path: <usersettings>\public\oem\nightlybuilds\2.0.904.3

            if (!Directory.Exists(sourceDocsFolder) && Directory.Exists(outputDocumentationFolder)) {
                // Doc's already moved?
                AssureLatestDocumentaionFolder(versionSource.Edition, versionSource.Channel);
                return false;
            }


            if (Directory.Exists(outputDocumentationFolder)) {
                LogWriter.WriteLine(string.Format("WARNING: Documentation folder {0} already exists.", outputDocumentationFolder));
                return false;
            }

            try {
                // Move documentation folder
                Directory.Move(sourceDocsFolder, outputDocumentationFolder);

                Db.Transact(() => {
                    versionSource.DocumentationFolder = outputDocumentationFolder;
                });

                // Also copy VersionInfo.xml to the document destination folder
                string versionInfoFile = Path.Combine(versionSource.SourceFolder, StarcounterEnvironment.FileNames.VersionInfoFileName);
                string versionInfoDestinationFile = Path.Combine(outputDocumentationFolder, StarcounterEnvironment.FileNames.VersionInfoFileName);
                File.Copy(versionInfoFile, versionInfoDestinationFile);


                LogWriter.WriteLine(string.Format("NOTICE: Documentation for {0}-{1}-{2} was moved to {3}.", versionSource.Edition, versionSource.Channel, versionSource.Version, outputDocumentationFolder));
                return true;
            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Failed to move the documentation folder {0} to {1}. {2}", sourceDocsFolder, outputDocumentationFolder, e.Message));
                return false;
            }

        }


        /// <summary>
        /// Get documentation folder for an edition and channel
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal static string GetDocumentationFolder(string edition, string channel) {

            if (string.IsNullOrEmpty(edition) || string.IsNullOrEmpty(channel)) {
                LogWriter.WriteLine(string.Format("ERROR: Invalid edition or channel argument."));
                return null;
            }

            // Add 'public'  (Static resource)
            string folder = Path.Combine(VersionHandlerApp.Settings.DocumentationFolder, "public");

            // Add doc name
            folder = Path.Combine(folder, "doc");

            // Add edition name
            folder = Path.Combine(folder, edition);

            // Add channel name
            folder = Path.Combine(folder, channel);

            return folder;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal static void AssureLatestDocumentaionFolder(string edition, string channel) {

            if (string.IsNullOrEmpty(edition) || string.IsNullOrEmpty(channel)) {
                LogWriter.WriteLine(string.Format("ERROR: Invalid edition or channel argument."));
                return;
            }

            VersionSource latest = GetCurrentLatestDocumentation(edition, channel);
            if (latest == null) {
                SetLatestDocumentation(edition, channel, null);
            }

            // Get latest version source that has documentation
            VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.IsAvailable=? AND o.DocumentationFolder IS NOT NULL ORDER BY o.VersionDate DESC", edition, channel, true).First;
            if (versionSource == null) {
                SetLatestDocumentation(edition, channel, null);
                return;
            }


            // Update latest documentaion
            if (latest == null || versionSource.VersionDate > latest.VersionDate) {
                SetLatestDocumentation(edition, channel, versionSource);
            }

        }


        /// <summary>
        /// Set the "latest" documention
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <param name="newVersionSource"></param>
        /// <returns></returns>
        internal static bool SetLatestDocumentation(string edition, string channel, VersionSource newVersionSource) {


            string documentationFolder = GetDocumentationFolder(edition, channel);
            string latestFolder = Path.Combine(documentationFolder, "latest");

            if (newVersionSource == null) {
                // Delete "latest folder" if it exists
                try {
                    if (Directory.Exists(latestFolder)) {
                        Directory.Delete(latestFolder, true);
                        LogWriter.WriteLine(string.Format("NOTIE: Removed invalid latest documentation {0}.", latestFolder));
                    }
                    return true;
                }
                catch (Exception e) {
                    LogWriter.WriteLine(string.Format("ERROR: Failed to remove documentation folder {0}. {1}.", latestFolder, e.Message));
                    return false;
                }

            }

            // Copy version documentation to latest folder
            try {

                DirectoryInfo source = new DirectoryInfo(newVersionSource.DocumentationFolder);

                // Remove current latest
                if (Directory.Exists(latestFolder)) {
                    Directory.Delete(latestFolder, true);
                }

                if (!Directory.Exists(latestFolder)) {
                    Directory.CreateDirectory(latestFolder);
                }

                DirectoryInfo destination = new DirectoryInfo(latestFolder);
                Utils.CopyFilesRecursively(source, destination);

                LogWriter.WriteLine(string.Format("NOTICE: New latest documentation set to {0}-{1}-{2}.", newVersionSource.Edition, newVersionSource.Channel, newVersionSource.Version));
                return true;
            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Failed to copy the documentation folder {0} to {1}. {2}.", newVersionSource.DocumentationFolder, latestFolder, e.Message));
                return false;
            }

        }


        /// <summary>
        /// Get current VersionSource of the "latest" version documentation that is in the "latest" folder
        /// </summary>
        /// <param name="edition"></param>
        /// <param name="channel"></param>
        /// <returns></returns>
        internal static VersionSource GetCurrentLatestDocumentation(string edition, string channel) {

            try {

                string documentationFolder = GetDocumentationFolder(edition, channel);
                if (documentationFolder == null) {
                    return null;
                }

                // Add latest
                string versionInfoFile = Path.Combine(documentationFolder, "latest");
                versionInfoFile = Path.Combine(versionInfoFile, StarcounterEnvironment.FileNames.VersionInfoFileName);

                if (!File.Exists(versionInfoFile)) {
                    LogWriter.WriteLine(string.Format("WARNING: Missing documentation information file {0}", versionInfoFile));
                    return null;
                }

                string doc_edition = string.Empty;
                string doc_channel = string.Empty;
                string doc_version = string.Empty;
                DateTime doc_sourceUTCDate = DateTime.MinValue;

                using (FileStream fs = File.OpenRead(versionInfoFile)) {
                    // Read version info
                    PackageHandler.ReadInfoVersionInfo(fs, out doc_edition, out doc_channel, out doc_version, out doc_sourceUTCDate);

                    // Validate version info
                    bool valid = doc_edition != string.Empty && doc_channel != string.Empty && doc_version != string.Empty && doc_sourceUTCDate != DateTime.MinValue;

                    if (valid == false) {
                        LogWriter.WriteLine(string.Format("ERROR: Invalid {0} {1}", StarcounterEnvironment.FileNames.VersionInfoFileName, versionInfoFile));
                        return null;
                    }
                }


                // Check if the latest documentation version source exists
                return Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Edition=? AND o.Channel=? AND o.Version=? AND o.IsAvailable=? AND o.DocumentationFolder IS NOT NULL ORDER BY o.VersionDate DESC", doc_edition, doc_channel, doc_version, true).First;

            }
            catch (Exception e) {
                LogWriter.WriteLine(string.Format("ERROR: Getting ({0}-{1}-latest documentation version information. {2}.", edition, channel, e.Message));
                return null;
            }

        }

    }
}
