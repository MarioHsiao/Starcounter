using System;
using Starcounter;

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
        /// Version object of the source
        /// </summary>
        public Version VersionObject {
            get {
                return new Version(this.Version);
            }
        }

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

            // Get Latest all available versions
            var result = Db.SlowSQL<String>("SELECT o.Version FROM VersionBuild o WHERE o.Channel=? GROUP BY o.Version", channel);

            String highestVersion = string.Empty;

            // Find highest version number
            foreach (var item in result) {
                if (highestVersion == string.Empty || new Version(item) > new Version(highestVersion)) {
                    highestVersion = item;
                }
            }

            VersionBuild latestBuild = null;

            // Get latest available build
            if (highestVersion != string.Empty) {
                latestBuild = Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.Version=? AND o.HasBeenDownloaded=?", highestVersion, false).First;
            }

            return latestBuild;
        }


        /// <summary>
        /// Get an available build of a specific version
        /// </summary>
        /// <param name="version"></param>
        /// <returns>Unique build or null</returns>
        internal static VersionBuild GetAvilableBuild(string version) {
            return Db.SlowSQL<VersionBuild>("SELECT o FROM VersionBuild o WHERE o.Version=? AND o.HasBeenDownloaded=?", version, false).First;
        }

    }
}
