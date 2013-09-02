using Starcounter;
using StarcounterApplicationWebSocket.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
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
        /// <param name="builderTool"></param>
        /// <returns></returns>
        internal static bool ReadPackageInfo(string file, out string version, out string channel, out string builderTool) {

            version = string.Empty;
            channel = string.Empty;
            builderTool = string.Empty;

            using (ZipArchive archive = ZipFile.OpenRead(file)) {
                foreach (ZipArchiveEntry entry in archive.Entries) {
                    if (entry.FullName.Equals("versioninfo.xml", StringComparison.OrdinalIgnoreCase)) {
                        Stream s = entry.Open();

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(s);

                        XmlNodeList versionInfo = xmlDoc.GetElementsByTagName("VersionInfo");
                        if (versionInfo != null && versionInfo.Count > 0) {
                            XmlNodeList nodeList = versionInfo[0].SelectNodes("Version");
                            if (nodeList != null && nodeList.Count > 0) {
                                version = nodeList[0].InnerText;
                            }
                            nodeList = versionInfo[0].SelectNodes("Channel");
                            if (nodeList != null && nodeList.Count > 0) {
                                channel = nodeList[0].InnerText;
                            }
                        }
                        s.Close();
                        s.Dispose();
                    }
                    else if (entry.FullName.Equals("GenerateInstaller.exe", StringComparison.OrdinalIgnoreCase)) {
                        builderTool = "GenerateInstaller.exe";
                    }

                }
            }

            return version != string.Empty && channel != string.Empty && builderTool != string.Empty;
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

                string version = string.Empty;
                string channel = string.Empty;
                string builderTool = string.Empty;

                if (!File.Exists(file)) {
                    throw new FileNotFoundException("Can not find the file", file);
                }

                // Validate the file.
                bool result = PackageHandler.ReadPackageInfo(file, out version, out channel, out builderTool);

                if (result == false) {
                    message = "Invalid package content.";

                    if (version == string.Empty) {
                        message += message + " Missing <Version> tag information.";
                    }
                    if (channel == string.Empty) {
                        message += message + " Missing <Channel> tag information.";
                    }
                    if (builderTool == string.Empty) {
                        message += message + " Missing builder tool GenerateInstaller.exe";
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
                VersionSource dupCheckResult = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE o.Version=?", version).First;
                if (dupCheckResult != null) {
                    message = string.Format("Source version {0} already exist in database", version);

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
                Db.Transaction(() => {
                    VersionSource versionSource = new VersionSource();
                    versionSource.SourceFolder = null;
                    versionSource.PackageFile = file;
                    versionSource.Channel = channel;
                    versionSource.Version = version;
                    versionSource.BuildError = false;
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

    }
}
