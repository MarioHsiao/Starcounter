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
        /// Folder where the source is located
        /// </summary>
        public string SourceFolder;


        /// <summary>
        /// Full path to the Version Package file
        /// </summary>
        /// <remarks>This is empty if the package has been unpacked</remarks>
        public string PackageFile;


        /// <summary>
        /// If source can not be built
        /// </summary>
        public bool BuildError;

    }
}
