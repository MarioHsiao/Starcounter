using System;
using Starcounter;


namespace StarcounterApplicationWebSocket.VersionHandler.Model {

    /// <summary>
    /// Available sources
    /// </summary>
    [Database]
    public class VersionSource {

        /// <summary>
        /// Version of the source
        /// </summary>
        public string Version;


        /// <summary>
        /// Channel of the source, example 'Stable', 'NightlyBuilds'
        /// </summary>
        public string Channel;


        /// <summary>
        /// UTC Date of the Version
        /// </summary>
        public DateTime VersionDate;


        /// <summary>
        /// Folder where the source is located
        /// </summary>
        public string SourceFolder;


        /// <summary>
        /// Folder where the documentation is located
        /// </summary>
        public string DocumentationFolder;

        /// <summary>
        /// Full path to the Version Package file
        /// </summary>
        /// <remarks>This is empty if the package has been unpacked</remarks>
        public string PackageFile;


        /// <summary>
        /// If source can not be built
        /// </summary>
        public bool BuildError;

        /// <summary>
        /// Get the latest version number in a specific channel
        /// </summary>
        /// <param name="channel">Channel name</param>
        /// <returns>Version or null if no valid versions was found in the specified channel</returns>
        internal static Version GetLatestVersion(string channel) {

            // Get Latest all available versions
            var result = Db.SlowSQL<String>("SELECT o.Version FROM VersionSource o WHERE o.BuildError=? AND o.Channel=? GROUP BY o.Version", false, channel);

            Version latestVersion = new Version();

            // Find highest version number
            foreach (var item in result) {
                Version currentVersion = new Version(item);
                int compResult = currentVersion.CompareTo(latestVersion);
                if (compResult > 0) {
                    latestVersion = currentVersion;
                }
            }

            // If there is no versionse then return null
            if (latestVersion.Equals(new Version())) {
                return null;
            }

            return latestVersion;
        }

    }
}
