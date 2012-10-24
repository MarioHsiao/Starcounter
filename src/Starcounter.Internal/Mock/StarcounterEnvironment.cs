// ***********************************************************************
// <copyright file="StarcounterEnvironment.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter
{

    /// <summary>
    /// Class VersionInfo
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public string Configuration { get; set; }
        /// <summary>
        /// Gets or sets the platform.
        /// </summary>
        /// <value>The platform.</value>
        public string Platform { get; set; }
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public Version Version { get; set; }
        /// <summary>
        /// Gets or sets the ID full base32.
        /// </summary>
        /// <value>The ID full base32.</value>
        public string IDFullBase32 { get; set; }
        /// <summary>
        /// Gets or sets the ID tail base64.
        /// </summary>
        /// <value>The ID tail base64.</value>
        public string IDTailBase64 { get; set; }
        /// <summary>
        /// Gets or sets the ID tail decimal.
        /// </summary>
        /// <value>The ID tail decimal.</value>
        public UInt32 IDTailDecimal { get; set; }
        /// <summary>
        /// Gets or sets the required registration date.
        /// </summary>
        /// <value>The required registration date.</value>
        public DateTime RequiredRegistrationDate { get; set; }

        // Setting default version settings.
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionInfo" /> class.
        /// </summary>
        public VersionInfo()
        {
            Configuration = "unknown";
            Platform = "unknown";
            Version = new Version(0, 0, 0, 0);
            IDFullBase32 = "000000000000000000000000";
            IDTailBase64 = "0000000";
            IDTailDecimal = 0;
            RequiredRegistrationDate = new DateTime(1900, 1, 1);
        }
    }


    /// <summary>
    /// Class StarcounterEnvironment
    /// </summary>
    public static class StarcounterEnvironment
    {
        /// <summary>
        /// The system directory
        /// </summary>
        public static string SystemDirectory;

        /// <summary>
        /// Represents an unknown Starcounter version.
        /// </summary>
        public static readonly VersionInfo UnknownVersionInfo = new VersionInfo();

        /// <summary>
        /// Gets the version info.
        /// </summary>
        /// <returns>VersionInfo.</returns>
        public static VersionInfo GetVersionInfo()
        {
            return UnknownVersionInfo;
        }

        /// <summary>
        /// Class DefaultPorts
        /// </summary>
        public static class DefaultPorts
        {
            /// <summary>
            /// The SQL prolog
            /// </summary>
            public const int SQLProlog = 8011;
        }

        /// <summary>
        /// Class SpecialVariables
        /// </summary>
        public static class SpecialVariables
        {
            /// <summary>
            /// Scs the conn max hits per page.
            /// </summary>
            /// <returns>System.UInt32.</returns>
            /// <exception cref="System.NotImplementedException"></exception>
            public static uint ScConnMaxHitsPerPage()
            {
                throw new System.NotImplementedException();
            }
        }

        /// <summary>
        /// Class VariableNames
        /// </summary>
        public static class VariableNames
        {
            /// <summary>
            /// The installation directory
            /// </summary>
            public static string InstallationDirectory;
        }

        /// <summary>
        /// Class InternetAddresses
        /// </summary>
        public static class InternetAddresses
        {
            /// <summary>
            /// The starcounter web site
            /// </summary>
            public const string StarcounterWebSite = "http://www.starcounter.com";
            /// <summary>
            /// The starcounter forum
            /// </summary>
            public const string StarcounterForum = "http://www.starcounter.com/forum/";
            /// <summary>
            /// The starcounter wiki
            /// </summary>
            public const string StarcounterWiki = "http://www.starcounter.com/wiki/";
            /// <summary>
            /// The administrator start page
            /// </summary>
            public const string AdministratorStartPage = "http://www.starcounter.com/admin/index.php";
        }
    }
}