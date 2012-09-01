
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