using System;
using Starcounter;

namespace StarcounterApplicationWebSocket.VersionHandler.Model {

    /// <summary>
    /// Version Handler Application Settings
    /// </summary>
    [Database]
    public class VersionHandlerSettings {

        /// <summary>
        /// Folder where to store uploaded files
        /// </summary>
        public string UploadFolder;


        /// <summary>
        /// Folder where the source is located for all version
        /// </summary>
        public string SourceFolder;


        /// <summary>
        /// Folder where unique builds versions is stored
        /// </summary>
        public string VersionFolder;


        /// <summary>
        /// Folder where the documentation is stored
        /// </summary>
        public string DocumentationFolder;


        /// <summary>
        /// Log file
        /// </summary>
        public string LogFile;


        /// <summary>
        /// Full path to the Certification File
        /// </summary>
        public string CertificationFile;


        /// <summary>
        /// Default Maximum availabe builds for a version
        /// </summary>
        public int MaximumBuilds;


        /// <summary>
        /// Get Settings
        /// </summary>
        /// <returns>If no settings exits, default settings will be created and used</returns>
        static public VersionHandlerSettings GetSettings() {

            VersionHandlerSettings settings = Db.SlowSQL<VersionHandlerSettings>("SELECT o FROM VersionHandlerSettings o").First;

            Db.Transaction(() => {
                if (settings == null) {
                    // Create default settings
                    settings = new VersionHandlerSettings();
                    settings.MaximumBuilds = 10;
                }

                if (string.IsNullOrEmpty(settings.UploadFolder)) {
                    settings.UploadFolder = @"c:\versions\uploads";
                }
                if (string.IsNullOrEmpty(settings.SourceFolder)) {
                    settings.SourceFolder = @"c:\versions\source";
                }
                if (string.IsNullOrEmpty(settings.VersionFolder)) {
                    settings.VersionFolder = @"c:\versions\builds";
                }
                if (string.IsNullOrEmpty(settings.DocumentationFolder)) {
                    settings.DocumentationFolder = @"c:\versions\docs";
                }
                if (string.IsNullOrEmpty(settings.LogFile)) {
                    settings.LogFile = @"c:\versions\versionhandler.log";
                }
                if (string.IsNullOrEmpty(settings.CertificationFile)) {
                    settings.CertificationFile = @"c:\program files\starcounter\starcounter-2014.cer";
                }


            });

            return settings;
        }

    }

}
