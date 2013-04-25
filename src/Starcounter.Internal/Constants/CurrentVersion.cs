
using System;
using System.Diagnostics;

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
        /// Unique full ID in Base32 format.
        /// </summary>
        public const String IDFullBase32 = "000000000000000000000000";

        /// <summary>
        /// Unique tail ID in Base64 format.
        /// </summary>
        public const String IDTailBase64 = "0000000";

        /// <summary>
        /// Unique tail ID in decimal format.
        /// </summary>
        public const UInt32 IDTailDecimal = 0;

        /// <summary>
        /// Required registration date.
        /// </summary>
        public static readonly DateTime RequiredRegistrationDate = DateTime.Parse("1900-01-01");
    }
}