
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
        ////////////////////////////////////////////////
        // NOTE: Do not modify, even whitespace!!!
        // Used for direct replacement by installer.
        ////////////////////////////////////////////////

        /// <summary>
        /// This Starcounter version.
        /// </summary>
        public const String Version = "2.0.0.0";

        /// <summary>
        /// This Starcounter version date.
        /// </summary>
        public static readonly DateTime VersionDate = DateTime.Parse("1900-01-01 01:01:01Z",
            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}