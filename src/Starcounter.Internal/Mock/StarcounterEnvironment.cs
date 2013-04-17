// ***********************************************************************
// <copyright file="StarcounterEnvironment.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Xml;

namespace Starcounter.Internal
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
        /// Default network ports that are used by different Starcounter components.
        /// </summary>
        public static class DefaultPorts
        {
            /// <summary>
            /// Maximum size of ports range that can be used by each server.
            /// </summary>
            public const int ServerPortRangeSize = 128;

            /// <summary>
            /// Default port for the database process.
            /// </summary>
            public const int Database = 10500;

            /// <summary>
            /// Default port for the Prolog SQL parsing process.
            /// </summary>
            public const int SQLProlog = 8066;

            /// <summary>
            /// Default port for the activity monitor server.
            /// </summary>
            public const int ActivityMonitor = 9021;
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
        /// Environment variable names.
        /// </summary>
        public static class VariableNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the environment variable containing the path to the
            /// directory where Starcounter is installed.
            /// </summary>
            public const string InstallationDirectory = "StarcounterBin";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the environment variable containing the name of the
            /// default server on the current machine.
            /// </summary>
            public const string DefaultServer = "StarcounterServer";

            /// <summary>
            /// Provides the name of the environment variable key used to
            /// store the default personal server port for processes, users
            /// and/or machines.
            /// </summary>
            public const string DefaultServerPersonalPort = "StarcounterServerPersonalPort";

            /// <summary>
            /// Provides the name of the environment variable key used to
            /// store the default system server port for processes, users
            /// and/or machines.
            /// </summary>
            public const string DefaultServerSystemPort = "StarcounterServerSystemPort";
        }

        /// <summary>
        /// Special directories.
        /// </summary>
        public static class Directories
        {
            /// <summary>
            /// Full path to USER application data directory.
            /// </summary>
            public static readonly String UserAppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                StarcounterConstants.ProgramNames.ProductName);

            /// <summary>
            /// Full path to SYSTEM application data directory.
            /// </summary>
            public static readonly String SystemAppDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                StarcounterConstants.ProgramNames.ProductName);

            /// <summary>
            /// Weaver temp sub directory.
            /// </summary>
            public const String WeaverTempSubDirectory = "weaver";
        }

        /// <summary>
        /// Well known file names.
        /// </summary>
        public static class FileNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter GUI administrator program error log.
            /// </summary>
            public const string ClientErrorLog = "ClientErrors.log";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter GUI administrator program statistics
            /// collection file.
            /// </summary>
            public const string ClientStats = "Clientdata.dat";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter Server program statistics
            /// collection file.
            /// </summary>
            public const string ServerStats = "Serverdata.dat";

            /// <summary>
            /// Holds a constant read-only value representing the name
            /// of the Starcounter Server/Administrator version information
            /// NOTE: This code is duplicated in the installer
            /// \perforce\Starcounter\Dev\Yellow\Main\CoreComponents.Net\Starcounter.InstallerWPF\MainWindow.xaml.cs
            /// </summary>
            public const string Version = "VersionInfo.xml";

            /// <summary>
            /// Default collation Filename prefix
            /// Filename example: TurboText_en-GB_2.dll
            ///     Prefix: TurboText
            ///     Culture: en-GB
            ///     Version: 2
            /// </summary>
            public const String CollationFileNamePrefix = "TurboText";
        }

        /// <summary>
        /// Names of Starcounter servers.
        /// </summary>
        public static class ServerNames
        {
            /// <summary>
            /// Holds a constant read-only value representing the reserved
            /// name used for the default system level server created by
            /// the Starcounter install program on machines where this option
            /// is taken advantage of.
            /// </summary>
            /// <remarks>
            /// Custom servers created manually are not allowed to use this
            /// name.
            /// </remarks>
            public const string SystemServer = "System";

            /// <summary>
            /// Default name postfix for the system server
            /// (appended in brackets to displayed name).
            /// </summary>
            public const string SystemServerDisplayName = "System";

            /// <summary>
            /// Default name for the system server service.
            /// </summary>
            public const string SystemServerServiceName = "StarcounterSystemServer";

            /// <summary>
            /// Holds a constant read-only value representing the reserved
            /// name used for the default user level personal server created by
            /// the Starcounter install program on machines where this option
            /// is taken advantage of.
            /// </summary>
            /// <remarks>
            /// Custom servers created manually are not allowed to use this
            /// name.
            /// </remarks>
            public const string PersonalServer = "Personal";

            /// <summary>
            /// Investigates if the given server name is recognized as one of the
            /// standard reserved server names.
            /// </summary>
            /// <param name="serverName">Name to investigate.</param>
            /// <returns>True if the name is the name of a recognized server; false
            /// if not.</returns>
            public static bool IsRecognizedServerName(string serverName)
            {
                StringComparison comparisonMethod;

                if (string.IsNullOrEmpty(serverName))
                    throw new ArgumentNullException("serverName");

                comparisonMethod = StringComparison.InvariantCultureIgnoreCase;
                return
                    serverName.Equals(ServerNames.SystemServer, comparisonMethod) ||
                    serverName.Equals(ServerNames.PersonalServer, comparisonMethod);
            }
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

                /// <summary>
        /// Gets the Starcounter version information from the data stream or file.
        /// </summary>
        /// <returns>VersionInfo</returns>
        public static VersionInfo GetVersionInfo(StringReader textStreamReader, String filePath)
        {
            // Creating default VersionInfo (in case if reading operation fails).
            VersionInfo versionInfo = UnknownVersionInfo;
            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                // Load the XML document from the specified text stream or file path.
                if (textStreamReader != null)
                    xmlDoc.Load(textStreamReader);
                else
                    xmlDoc.Load(filePath);

                // Reading configuration.
                try
                {
                    XmlNodeList ConfigurationTags = xmlDoc.GetElementsByTagName("Configuration");
                    versionInfo.Configuration = ((XmlElement)ConfigurationTags[0]).InnerText; // e.g. Release
                }
                catch { }

                // Reading platform.
                try
                {
                    XmlNodeList PlatformTags = xmlDoc.GetElementsByTagName("Platform");
                    versionInfo.Platform = ((XmlElement)PlatformTags[0]).InnerText; // e.g. x64
                }
                catch { }

                // Reading version information.
                try
                {
                    XmlNodeList FullversionTags = xmlDoc.GetElementsByTagName("Version");
                    string versionStr = ((XmlElement)FullversionTags[0]).InnerText; // e.g. 2.0.0.0
                    try
                    {
                        // Creating with wrong version string can also throw an exception.
                        versionInfo.Version = new Version(versionStr);
                    }
                    catch { }
                }
                catch { }

                // Reading unique ID in Base32.
                try
                {
                    XmlNodeList IDFullBase32Tags = xmlDoc.GetElementsByTagName("IDFullBase32");
                    versionInfo.IDFullBase32 = ((XmlElement)IDFullBase32Tags[0]).InnerText; // e.g. 000000000000000000000000
                }
                catch { }

                // Reading unique ID tail in Base64.
                try
                {
                    XmlNodeList IDTailBase64Tags = xmlDoc.GetElementsByTagName("IDTailBase64");
                    versionInfo.IDTailBase64 = ((XmlElement)IDTailBase64Tags[0]).InnerText; // e.g. 0000000
                }
                catch { }

                // Reading unique ID tail in Decimal format.
                try
                {
                    XmlNodeList IDTailDecimalTags = xmlDoc.GetElementsByTagName("IDTailDecimal");
                    string IDTailDecimalStr = ((XmlElement)IDTailDecimalTags[0]).InnerText; // e.g. 0000000
                    UInt32 IDTailDecimal;
                    if (UInt32.TryParse(IDTailDecimalStr, out IDTailDecimal))
                    {
                        versionInfo.IDTailDecimal = IDTailDecimal;
                    }
                }
                catch { }

                // Reading required registration date.
                try
                {
                    XmlNodeList ReqRegDateTags = xmlDoc.GetElementsByTagName("RequiredRegistrationDate");
                    string ReqRegDateStr = ((XmlElement)ReqRegDateTags[0]).InnerText; // e.g. 2012-06-09
                    DateTime ReqRegDate = DateTime.ParseExact(ReqRegDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    versionInfo.RequiredRegistrationDate = ReqRegDate;
                }
                catch { }
            }
            catch { }
            // If any exception occurs default version settings will be loaded.

            return versionInfo;
        }

        /// <summary>
        /// Cached reference of Starcounter version information.
        /// </summary>
        private static VersionInfo ScVersionInfo = null;

        /// <summary>
        /// Represents an unknown Starcounter version.
        /// </summary>
        public static readonly VersionInfo UnknownVersionInfo = new VersionInfo();

        /// <summary>
        /// Gets the version info.
        /// </summary>
        public static VersionInfo GetVersionInfo()
        {
            // Only fetch the info once.
            if (ScVersionInfo != null)
                return ScVersionInfo;

            // Create default VersionInfo.
            ScVersionInfo = UnknownVersionInfo;

            try
            {
                // Checking if system directory is initialized.
                if (!String.IsNullOrEmpty(StarcounterEnvironment.SystemDirectory))
                {
                    // Getting version file from system directory.
                    String versionFilePath = Path.Combine(StarcounterEnvironment.SystemDirectory, StarcounterEnvironment.FileNames.Version);

                    // Checking that version file exists.
                    if (File.Exists(versionFilePath))
                        ScVersionInfo = GetVersionInfo(null, versionFilePath);
                }
            }
            catch { }

            return ScVersionInfo;
        }

        static string ReadSystemDirectoryFromEnvironment()
        {
            string candidate;
            string keyName;

            keyName = StarcounterEnvironment.VariableNames.InstallationDirectory;
            candidate = null;

            foreach (var target in new EnvironmentVariableTarget[] {
                EnvironmentVariableTarget.User,
                EnvironmentVariableTarget.Machine })
            {
                try
                {
                    candidate = Environment.GetEnvironmentVariable(keyName, target);
                }
                catch (SecurityException securityException)
                {
                    // Wrap the security exception in a custom exception with our
                    // code and raise it.

                    throw ErrorCode.ToException(
                        Error.SCERRENVVARIABLENOTACCESSIBLE,
                        securityException,
                        string.Format("Key={0}, Target={1}", keyName, Enum.GetName(typeof(EnvironmentVariableTarget), target))
                        );
                }

                if (!string.IsNullOrEmpty(candidate))
                    break;
            }

            if (string.IsNullOrEmpty(candidate))
            {
                // When requested, we expect the system directory to be resolved.
                // If it ain't, raise an exception.

                throw ErrorCode.ToException(Error.SCERRBINDIRENVNOTFOUND);
            }

            return candidate;
        }
    }
}