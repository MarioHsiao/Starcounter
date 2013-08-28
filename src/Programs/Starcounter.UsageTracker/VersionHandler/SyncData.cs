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

            // 1. Check if uploaded packages in 'Settings.UploadFolder' exist as Database.VersionPackage instances
            CheckVersionPackageVsDatabase();

            // 2. Check if Database Packages exist in 'Settings.UploadFolder'
            CheckDatabaseVsVersionPackages();

            // 3. Check if unpacked package source 'Settings.SourceFolder' exist in Database.Source class
            CheckSourcesVsDatabase();

            // 4. Check if Database Source exist in 'Settings.SourceFolder'
            CheckDatabaseVsSources();

            // 5. Check if versions in 'Settings.VersionFolder' exist in Database.Version class
            CheckBuildsVsDatabase();

            // 6. Check if Database.Version exist in 'Settings.VersionFolder'
            CheckDatabaseVsBuilds();

            // 7. Previously failed builds will be rebuild if necessary
            UnmarkFailedBuilds();

        }

        /// <summary>
        /// Check if uploads in the filesystem is registered in the database
        /// If upload is missing it will be added to the database if it's a vaild uploaded package
        /// and not a duplicate
        /// </summary>
        private static void CheckVersionPackageVsDatabase() {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;

            if (!Directory.Exists(settings.UploadFolder)) {
                return;
            }

            string[] files = Directory.GetFiles(settings.UploadFolder);
            foreach (string file in files) {

                VersionSource versionSource = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE PackageFile=?", file).First;
                if (versionSource == null) {
                    string message;
                    try {
                        bool result = PackageHandler.AddFileEntryToDatabase(file, out message);
                        if (result == false) {
                            LogWriter.WriteLine(string.Format("NOTICE: Can not add version package {0}. {1}", file, message));
                        }
                    }
                    catch (Exception e) {
                        LogWriter.WriteLine(string.Format("ERROR: Could not handle version package file {0}. {1}", file, e.Message));
                    }
                }

            }
        }

        /// <summary>
        /// Cleanup database entries that dosent exist on the filesystem
        /// </summary>
        private static void CheckDatabaseVsVersionPackages() {

            var result = Db.SlowSQL("SELECT o FROM VersionSource o");
            foreach (VersionSource source in result) {
                Db.Transaction(() => {

                    if (!string.IsNullOrEmpty(source.PackageFile) && !File.Exists(source.PackageFile)) {
                        source.PackageFile = string.Empty;
                        LogWriter.WriteLine(string.Format("NOTICE: Removing missing package file {0} from database", source.Version));
                    }

                    if (string.IsNullOrEmpty(source.PackageFile) && string.IsNullOrEmpty(source.SourceFolder)) {
                        LogWriter.WriteLine(string.Format("NOTICE: Removing source item {0} from database (package and sourcefolder is missing)", source.Version));
                        source.Delete();
                    }

                });


            }
        }

        private static void CheckSourcesVsDatabase() {

            VersionHandlerSettings settings = VersionHandlerApp.Settings;

            if (!Directory.Exists(settings.SourceFolder)) {
                return;
            }

            string[] channels = Directory.GetDirectories(settings.SourceFolder);

            foreach (string channel in channels) {

                string[] versionSources = Directory.GetDirectories(channel);

                foreach (string versionSource in versionSources) {

                    DirectoryInfo versionFolder = new DirectoryInfo(versionSource);

                    // TODO: Dont use the version folder name for version number.
                    // TODO: Extract the version from VersionInfo.xml

                    VersionSource source = Db.SlowSQL<VersionSource>("SELECT o FROM VersionSource o WHERE Version=?", versionFolder.Name).First;
                    if (source == null) {
                        LogWriter.WriteLine(string.Format("NOTICE: Adding missing version Source {0} to database", versionFolder.Name));

                        Db.Transaction(() => {
                            DirectoryInfo channelFolder = new DirectoryInfo(channel);
                            VersionSource dbSource = new VersionSource();
                            dbSource.SourceFolder = versionSource;
                            dbSource.PackageFile = string.Empty;
                            dbSource.Channel = channelFolder.Name;
                            dbSource.Version = versionFolder.Name;
                            dbSource.BuildError = false;
                        });
                    }
                }
            }
        }

        private static void CheckDatabaseVsSources() {

            var result = Db.SlowSQL("SELECT o FROM VersionSource o");

            foreach (VersionSource source in result) {

                Db.Transaction(() => {

                    if (!string.IsNullOrEmpty(source.SourceFolder) && !Directory.Exists(source.SourceFolder)) {
                        source.SourceFolder = string.Empty;
                        LogWriter.WriteLine(string.Format("NOTICE: Removing missing source version {0} from database", source.Version));
                    }


                    if (string.IsNullOrEmpty(source.PackageFile) && string.IsNullOrEmpty(source.SourceFolder)) {
                        LogWriter.WriteLine(string.Format("NOTICE: Removing source item {0} from database (sourcefolder and package is missing)", source.Version));
                        source.Delete();
                    }

                });

            }

            // TODO: Remove duplicates

        }

        private static void CheckBuildsVsDatabase() {

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
                            VersionBuild dbVersion = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.File=?", file).First;
                            if (dbVersion == null) {
                                bRemoveBuildFolder = true;
                            }
                            else {
                                // Also remove downloaded builds
                                if (dbVersion.HasBeenDownloaded == true) {
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
                                    LogWriter.WriteLine(string.Format("NOTICE: build Folder {0} was removed, File was downloaded or the file is missing in the database.", buildFolder));
                                }
                            }
                            catch (Exception e) {
                                LogWriter.WriteLine(string.Format("ERROR: Could not cleanup build folder {0}. {1}", buildFolder, e.Message));
                            }
                        }
                    }
                }
            }

        }

        private static void CheckDatabaseVsBuilds() {

            // Remove all builds that dosent have a source and has not been downloaded
            Db.Transaction(() => {
                var result = Db.SlowSQL("SELECT o FROM VersionBuild o WHERE o.Source is null AND o.HasBeenDownloaded=?", false);
                foreach (VersionBuild build in result) {
                    build.Delete();
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
                            LogWriter.WriteLine(string.Format("NOTICE: Missing build {0} has been removed from the database.", file));
                        }
                    }

                }
            });




        }

        /// <summary>
        /// Reset builds that faild so it will try to build again
        /// </summary>
        private static void UnmarkFailedBuilds() {

            var result = Db.SlowSQL("SELECT o FROM VersionSource o WHERE o.BuildError=?", true);
            Db.Transaction(() => {
                foreach (VersionSource source in result) {
                    source.BuildError = false;
                }
            });

        }

    }
}
