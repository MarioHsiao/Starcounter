
using System;

namespace Starcounter
{

    public class DbException : System.Exception
    {

        public DbException(uint e, string message) { }

        public uint ErrorCode { get { throw new System.NotImplementedException(); } }
    }

    public class LogSources
    {
        public static LogSource Sql = new LogSource("Sql");
    }

    public static class StarcounterEnvironment
    {
        public static string SystemDirectory;

        /// <summary>
        /// Represents an unknown Starcounter version.
        /// </summary>
        public static readonly VersionInfo UnknownVersionInfo = new VersionInfo();

        /// <summary>
        /// Gets the version info.
        /// </summary>
        public static VersionInfo GetVersionInfo() {
            return UnknownVersionInfo;
        }

        public static class DefaultPorts
        {
            public const int SQLProlog = 8011;
        }

        public static class SpecialVariables
        {
            public static uint ScConnMaxHitsPerPage()
            {
                throw new System.NotImplementedException();
            }
        }

        public static class VariableNames
        {
            public static string InstallationDirectory;
        }

        public static class InternetAddresses {
            public const string StarcounterWebSite = "http://www.starcounter.com";
            public const string StarcounterForum = "http://www.starcounter.com/forum/";
            public const string StarcounterWiki = "http://www.starcounter.com/wiki/";
            public const string AdministratorStartPage = "http://www.starcounter.com/admin/index.php";
        }
    }

    /// <summary>
    /// Class representing Starcounter version.
    /// </summary>
    public class VersionInfo {
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public Version Version { get; set; }
        public string IDFullBase32 { get; set; }
        public string IDTailBase64 { get; set; }
        public UInt32 IDTailDecimal { get; set; }
        public DateTime RequiredRegistrationDate { get; set; }

        // Setting default version settings.
        public VersionInfo() {
            Configuration = "unknown";
            Platform = "unknown";
            Version = new Version(0, 0, 0, 0);
            IDFullBase32 = "000000000000000000000000";
            IDTailBase64 = "0000000";
            IDTailDecimal = 0;
            RequiredRegistrationDate = new DateTime(1900, 1, 1);
        }
    }

    public class SqlConnectivity
    {
        public static uint InitSqlFunctions()
        {
            return 0;
        }

        public static void ThrowConvertedServerError(uint ec)
        {
            throw new System.Exception(ec.ToString());
        }

        public static string GetServerProfilingString()
        {
            return null;
        }
    }

}