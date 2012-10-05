
using Starcounter.Logging;
using System;

namespace Starcounter
{
    public class LogSources
    {
        public static LogSource Sql = new LogSource("Sql");
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