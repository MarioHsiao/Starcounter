using System;
using Starcounter;
using System.IO;
using Starcounter.Applications.UsageTrackerApp;

namespace StarcounterApplicationWebSocket.VersionHandler.Model {

    /// <summary>
    /// Unique builds
    /// </summary>
    [Database]
    public class VersionBuild {

        /// <summary>
        /// Version of the source
        /// </summary>
        public string Version;


        /// <summary>
        /// Channel of the source, example 'Stable', 'Nightly'
        /// </summary>
        public string Channel;


        /// <summary>
        /// Download date (UTC) when the download was initiated
        /// </summary>
        /// <remarks>If the download wa aborted this date will still be set</remarks>
        public DateTime DownloadDate;


        /// <summary>
        /// Date (UTC) when the version was build
        /// </summary>
        public DateTime BuildDate;


        /// <summary>
        /// Unique code assigned for identification of a single build
        /// </summary>
        /// <remarks>aka 'downloadid"</remarks>
        public string Serial;


        /// <summary>
        /// Download started by
        /// </summary>
        public string IPAdress;


        /// <summary>
        /// Full path to a unique build
        /// </summary>
        public string File;


        /// <summary>
        /// Reference to the source
        /// </summary>
        public VersionSource Source;


        /// <summary>
        /// True if the download has been started otherwise false
        /// </summary>
        public bool HasBeenDownloaded {
            get {
                return this.DownloadDate != DateTime.MinValue;
            }
        }


        /// <summary>
        /// Unique generated key
        /// </summary>
        /// <remarks>This key is connected to a somebody with an email</remarks>
        public string DownloadKey;


        /// <summary>
        /// Get the latest (Higest version number) of a version in a specific channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns>Unique build or null</returns>
        internal static VersionBuild GetLatestAvailableBuild(string channel) {

            // Get latest version source
            VersionSource versionSource = VersionSource.GetLatestVersion(channel);
            if (versionSource == null) return null;

            // Get version build
            return Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.Channel=? AND o.Version=? AND o.HasBeenDownloaded=?", versionSource.Channel, versionSource.Version, false).First;
        }


        /// <summary>
        /// Get an available build of a specific version
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="version"></param>
        /// <returns>Unique build or null</returns>
        internal static VersionBuild GetAvailableBuild(string channel, string version) {
            return Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.Channel=? AND o.Version=? AND o.HasBeenDownloaded=?", channel, version, false).First;
        }


        /// <summary>
        /// This will delete the Version build from filesystem and it's database reference
        /// if the version build has been downloaded the database entry will NOT be deleted.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="version"></param>
        internal static void DeleteVersionBuild(string channel, string version) {

            Db.Transaction(() => {

                // A downloaded version build should not be deleted
                SqlResult<VersionBuild> versionBuilds = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE  o.Channel=? AND o.Version=?", channel, version);
                foreach (VersionBuild versionBuild in versionBuilds) {

                    VersionBuild.DeleteVersionBuildFile(versionBuild);

                    if (!versionBuild.HasBeenDownloaded) {
                        // Also delete the database entry if the build hasent been downloaded
                        versionBuild.Delete();
                    }

                }

                // Delete folder if it's empty
                // c:\versions\builds\NightlyBuilds\2.0.986.3\
                string versionFolder = Path.Combine(VersionHandlerSettings.GetSettings().VersionFolder, channel);
                versionFolder = Path.Combine(versionFolder, version);

                try {

                    if (Directory.Exists(versionFolder)) {
                        if (StarcounterApplicationWebSocket.API.Versions.Utils.IsDirectoryEmpty(versionFolder)) {
                            Directory.Delete(versionFolder);
                        }
                    }

                }
                catch (Exception e) {
                    LogWriter.WriteLine(string.Format("ERROR: Failed to delete build folder {0}. {1}", versionFolder, e.Message));
                }

            });
        }


        /// <summary>
        /// Delete the build file/folder and remove it's database reference
        /// </summary>
        /// <param name="versionBuild"></param>
        /// <returns></returns>
        /// <returns>True if successfull otherwise false</returns>
        internal static bool DeleteVersionBuildFile(VersionBuild versionBuild) {

            if (!string.IsNullOrEmpty(versionBuild.File)) {

                try {
                    if (System.IO.File.Exists(versionBuild.File)) {
                        System.IO.File.Delete(versionBuild.File);
                    }

                    string folder = Path.GetDirectoryName(versionBuild.File);
                    if (Directory.Exists(folder)) {
                        Directory.Delete(folder, true);
                    }
                    Db.Transaction(() => {
                        versionBuild.File = null;
                    });

                    return true;
                }
                catch (Exception e) {
                    LogWriter.WriteLine(string.Format("ERROR: Failed to delete file {0}. {1}", versionBuild.File, e.Message));
                }

            }
            return false;
        }


    }


    /// <summary>
    /// 
    /// </summary>
    [Database]
    public class IPLocation {
        /// <summary>
        /// 
        /// </summary>
        public string IPAdress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CountryCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CountryName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RegionCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RegionName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ZipCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal Latitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public decimal Longitude { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MetroCode { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string AreaCode { get; set; }

    }
}
