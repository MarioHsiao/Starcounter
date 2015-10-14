
using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml;

namespace Starcounter.Internal
{
    /// <summary>
    /// StarcounterConstants
    /// </summary>
    public static class CurrentVersion
    {
        ///////////////////////////////////////////////////
        // WARNING!!!Do not modify, even whitespace here!!!
        // Used for direct replacement by installer.
        ///////////////////////////////////////////////////

        /// <summary>
        /// This Starcounter version.
        /// </summary>
        public static readonly String Version = "2.0.0";

        /// <summary>
        /// Internal channel name.
        /// </summary>
        static String channelName_ = null;

        /// <summary>
        /// This name of channel for this version.
        /// </summary>
        public static String ChannelName {
            get {

                if (null != channelName_)
                    return channelName_;

                XmlDocument versionXML = new XmlDocument();
                String versionInfoFilePath = System.IO.Path.Combine(StarcounterEnvironment.InstallationDirectory,
                    StarcounterEnvironment.FileNames.VersionInfoFileName);

                // Checking that version file exists and loading it.
                versionXML.Load(versionInfoFilePath);

                // NOTE: We are getting only first element.
                channelName_ = (versionXML.GetElementsByTagName("Channel"))[0].InnerText;

                return channelName_;
            }
        }

        /// <summary>
        /// This name of edition for this version.
        /// </summary>
        public static readonly String EditionName = "Starcounter";

        /// <summary>
        /// This Starcounter version date.
        /// </summary>
        public static readonly DateTime VersionDate = DateTime.Parse("1900-01-01 01:01:01Z",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    }
}