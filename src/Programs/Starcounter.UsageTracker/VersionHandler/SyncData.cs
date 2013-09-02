using Starcounter;
using StarcounterApplicationWebSocket.VersionHandler;
using StarcounterApplicationWebSocket.VersionHandler.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Applications.UsageTrackerApp.VersionHandler {
    /// <summary>
    /// Sync data between filsystem and database
    /// This is used as a recovery option if files
    /// where moved/deleted and added manually
    /// </summary>
    internal class SyncData {


        internal static void Start() {


            // Check if Database Packages exist in 'Settings.UploadFolder'
            // NOTE: It's important that this is called before .FixUnhandledPackages();
            RemoveInvalidSourceItems();

            // Check if uploaded packages in 'Settings.UploadFolder' exist as Database.VersionPackage instances
            FixUnhandledPackages();

            // Check if unpacked package source 'Settings.SourceFolder' exist in Database.Source class
            FixMissingSources();

            // Check if versions in 'Settings.VersionFolder' exist in Database.Version class
            CleanupBuildFolder();

            // Check if Database.Version exist in 'Settings.VersionFolder'
            CleanupBuilds();

            // Previously failed builds will be rebuild if necessary
            UnmarkFailedBuilds();

        }

        /// <summary>
        /// Remove invalid source items
        /// There is no package to unpack and there is no source folder specified
        /// </summary>
        private static void RemoveInvalidSourceItems() {

            Db.Transaction(() => {

                var result = Db.SlowSQL("SELECT o FROM VersionSource o");
                foreach (VersionSource source in result) {

                    if (!string.IsNullOrEmpty(source.PackageFile) && !File.Exists(source.PackageFile)) {
                        // Package file is missing
                        source.PackageFile = null;
                        LogWriter.WriteLine(string.Format("NOTICE: Removing missing package {0} from database.", source.Version));
                    }

                    if (!string.IsNullOrEmpty(source.SourceFolder) && !Directory.Exists(source.SourceFolder)) {
                        // Source folder is missing
                        source.SourceFolder = null;
                        LogWriter.WriteLine(string.Format("NOTICE: Removing missing source {0} from database.", source.Version));
                    }

                    if (string.IsNullOrEmpty(source.PackageFile) && string.IsNullOrEmpty(source.SourceFolder)) {
                        // No Package file specifed and there is no sourcefolder specified
                        string version = source.Version;
                        source.Delete();
                        LogWriter.WriteLine(string.Format("WARNING: Source item {0} was removed from database, package and sourcefolder was not specified.", version));
                    }
                }
            });

            // TODO: Remove duplicates

        }

        /// <summary>
        /// Check if packages in the filesystem is registered in the database
        /// If package is missing it will be added to the database if it's a vaild uploaded package
        /// and not a duplicate
        /// </summary>
        /// <remarks>
        /// A packages is a zipped file.
        /// </remarks>
        private static void FixUnhandledPackages() {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;

            if (!Directory.Exists(settings.UploadFolder)) {
                return;
            }

            string[] files = Directory.GetFiles(settings.UploadFolder);
            foreach (string file in files) {

                VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE PackageFile=?", file).First;
                if (versionSource == null) {
                    // Package file is not registred in database
                    string message;
                    try {
                        // Register package
                        bool result = PackageHandler.AddFileEntryToDatabase(file, out message);
                        if (result == false) {
                            LogWriter.WriteLine(string.Format("NOTICE: Failed to register package {0}. {1}.", file, message));
                        }
                    }
                    catch (Exception e) {
                        LogWriter.WriteLine(string.Format("ERROR: Failed to handle package {0}. {1}.", file, e.Message));
                    }
                }

            }
        }


        /// <summary>
        /// Check for missing source items.
        /// </summary>
        private static void FixMissingSources() {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;

            if (!Directory.Exists(settings.SourceFolder)) {
                return;
            }

            string[] channels = Directory.GetDirectories(settings.SourceFolder);

            foreach (string channel in channels) {

                string[] versionSources = Directory.GetDirectories(channel);

                foreach (string versionSourceFolder in versionSources) {
                    // ..\nightlybuilds\2.0.0.0\sdflsdljfsdl

                    // TODO: Dont use the version folder name for version number.
                    DirectoryInfo versionFolder = new DirectoryInfo(versionSourceFolder);

                    // TODO: Extract the version from VersionInfo.xml it's more accurat

                    VersionSource source = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE Version=?", versionFolder.Name).First;
                    if (source == null) {

                        Db.Transaction(() => {
                            DirectoryInfo channelFolder = new DirectoryInfo(channel);
                            VersionSource versionSource = new VersionSource();
                            versionSource.SourceFolder = versionSourceFolder;
                            versionSource.PackageFile = null;
                            versionSource.Channel = channelFolder.Name;
                            versionSource.Version = versionFolder.Name;
                            versionSource.BuildError = false;
                        });

                        LogWriter.WriteLine(string.Format("NOTICE: Missing source {0} was added to database.", versionFolder.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Cleanup build folder
        /// Downloaded files and files with no reference to database is removed
        /// </summary>
        private static void CleanupBuildFolder() {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;
            bool bRemoveBuildFolder = false;
            if (!Directory.Exists(settings.VersionFolder)) {
                return;
            }

            string[] channels = Directory.GetDirectories(settings.VersionFolder);

            foreach (string channel in channels) {

                string[] versions = Directory.GetDirectories(channel);
                DirectoryInfo channelFolder = new DirectoryInfo(channel);

                foreach (string version in versions) {

                    DirectoryInfo versionFolder = new DirectoryInfo(version);

                    string[] builds = Directory.GetDirectories(version);

                    foreach (string buildFolder in builds) {

                        bRemoveBuildFolder = false;
                        string[] files = Directory.GetFiles(buildFolder);
                        if (files == null || files.Length == 0) {
                            bRemoveBuildFolder = true;
                        }
                        else {
                            string file = files[0];
                            VersionBuild versionBuild = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.File=?", file).First;
                            if (versionBuild == null) {
                                bRemoveBuildFolder = true;
                            }
                            else {
                                // Also remove downloaded builds to cleanup
                                if (versionBuild.HasBeenDownloaded == true) {
                                    bRemoveBuildFolder = true;
                                }
                            }
                        }

                        // Delete folder
                        if (bRemoveBuildFolder) {
                            try {
                                // Cleanup
                                if (Directory.Exists(buildFolder)) {
                                    Directory.Delete(buildFolder, true);
                                    LogWriter.WriteLine(string.Format("NOTICE: Build {0} was removed, File was downloaded or there was no reference to it.", buildFolder));
                                }
                            }
                            catch (Exception e) {
                                LogWriter.WriteLine(string.Format("ERROR: Failed to cleanup build folder {0}. {1}.", buildFolder, e.Message));
                            }
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Remove build's that dosent have a source (we can not build more)
        /// and remove build where there is no reference to an existing file
        /// </summary>
        private static void CleanupBuilds() {

            // Remove all builds that dosent have a source and has not been downloaded
            Db.Transaction(() => {
                var result = Db.SlowSQL("SELECT o FROM VersionBuild o WHERE o.Source is null AND o.HasBeenDownloaded=?", false);
                foreach (VersionBuild build in result) {
                    string file = build.File;
                    build.Delete();
                    LogWriter.WriteLine(string.Format("NOTICE: Removed build {0} from database, Build have not source.", file));
                }
            });

            Db.Transaction(() => {

                var result = Db.SlowSQL("SELECT o FROM VersionBuild o");
                foreach (VersionBuild build in result) {

                    if (!File.Exists(build.File)) {

                        // Do not delete downloaded build, we like to track them
                        if (!build.HasBeenDownloaded) {
                            string file = build.File;
                            build.Delete();
                            LogWriter.WriteLine(string.Format("NOTICE: Removed build {0} from database, File was missing.", file));
                        }
                    }
                }
            });

        }

        /// <summary>
        /// Reset builds that faild so it will try to build again
        /// </summary>
        private static void UnmarkFailedBuilds() {

            // TODO: Have a build retry counter. 

            var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=?", true);
            Db.Transaction(() => {
                foreach (VersionSource source in result) {
                    source.BuildError = false;
                    LogWriter.WriteLine(string.Format("NOTICE: Resetted source {0} build error flag.", source.Version));
                }
            });

        }

    }
}
