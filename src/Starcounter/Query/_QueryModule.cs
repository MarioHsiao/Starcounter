
using System;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;
using System.Diagnostics;

namespace Starcounter.Query
{
    /// <summary>
    /// Configuration, initiation and termination of query module.
    /// </summary>
    public static class QueryModule
    {
        // Configuration of query module.
        static readonly String processFolder = StarcounterEnvironment.SystemDirectory + "\\32BitComponents\\";
        const String processFileName = "StarcounterSQL.exe";
        //const String processVersion = "111208";
        const String processVersion = "121002";
        static Int32 processPort;
        static Int32 configuredProcessPort = 0;
        //static readonly String schemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\schema.pl";
        static readonly String schemaFolder = AppDomain.CurrentDomain.BaseDirectory + "\\";
        const Int32 maxQueryLength = 3000;
        const Int32 maxQueryRetries = 10;
        const Int32 maxVerifyRetries = 100;
        const Int32 timeBetweenVerifyRetries = 100; // [ms]

        public static void Configure(Int32 sqlProcessPort)
        {
            configuredProcessPort = sqlProcessPort;
        }

        /// <summary>
        /// Initiates query module. Called during start-up.
        /// </summary>
        internal static void Initiate(Boolean notInUse)
        {
            // TEMP
            System.Diagnostics.Debugger.Break();

            // Connect managed and native Sql functions.
            UInt32 errCode = SqlConnectivity.InitSqlFunctions();

            // Checking for error code and translating it.
            if (errCode != 0)
                SqlConnectivity.ThrowConvertedServerError(errCode);

            // Start external SQL process (Prolog-process).
            processPort = configuredProcessPort;
            if (processPort == 0)
                processPort = StarcounterEnvironment.DefaultPorts.SQLProlog;
            PrologManager.Initiate(notInUse, processFolder, processFileName, processVersion, processPort, schemaFolder,
                maxQueryLength, maxQueryRetries, maxVerifyRetries, timeBetweenVerifyRetries);
        }

        /// <summary>
        /// Terminates query module. Called during shut-down.
        /// </summary>
        internal static void Terminate()
        {
            PrologManager.Terminate();
        }
    }
}
