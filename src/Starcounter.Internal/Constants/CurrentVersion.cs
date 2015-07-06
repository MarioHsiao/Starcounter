
using System;
using System.Diagnostics;
using System.Globalization;

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
        public static readonly String Version = "2.0.0.0";

        /// <summary>
        /// This name of channel for this version.
        /// </summary>
        public static readonly String ChannelName = "NightlyBuilds";

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