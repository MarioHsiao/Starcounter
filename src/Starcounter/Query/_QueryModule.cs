
using Starcounter.Binding;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;
using System;
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
        //static readonly String schemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\schema.pl";
        static readonly String schemaFolder = AppDomain.CurrentDomain.BaseDirectory + "\\";
        const Int32 maxQueryLength = 3000;
        const Int32 maxQueryRetries = 10;
        const Int32 maxVerifyRetries = 100;
        const Int32 timeBetweenVerifyRetries = 100; // [ms]

        /// <summary>
        /// Initiates query module. Called during start-up.
        /// </summary>
        /// <param name="sqlProcessPort">External SQL process port. If 0 then default should be used.</param>
        public static void Initiate(Int32 sqlProcessPort)
        {
            //// TEMP
            //System.Diagnostics.Debugger.Break();

            // Connect managed and native Sql functions.
            UInt32 errCode = SqlConnectivity.InitSqlFunctions();

            // Checking for error code and translating it.
            if (errCode != 0)
                SqlConnectivity.ThrowConvertedServerError(errCode);

            // Start external SQL process (Prolog-process).
            processPort = sqlProcessPort;
            if (processPort == 0)
                processPort = StarcounterEnvironment.DefaultPorts.SQLProlog;
            PrologManager.Initiate(processFolder, processFileName, processVersion, processPort, schemaFolder,
                maxQueryLength, maxQueryRetries, maxVerifyRetries, timeBetweenVerifyRetries);
        }

        /// <summary>
        /// Remove all schema information from external SQL process (Prolog-process).
        /// </summary>
        internal static void Reset()
        {
            try
            {
                Starcounter.ThreadHelper.SetYieldBlock();
                Scheduler scheduler = Scheduler.GetInstance();
                PrologManager.DeleteAllSchemaInfo(scheduler);
            }
            finally
            {
                Starcounter.ThreadHelper.ReleaseYieldBlock();
            }
        }

        internal static void UpdateSchemaInfo(TypeDef[] typeDefArray)
        {
            try
            {
                Starcounter.ThreadHelper.SetYieldBlock();
                Scheduler scheduler = Scheduler.GetInstance();
                PrologManager.ExportSchemaInfo(scheduler, typeDefArray);
            }
            finally
            {
                Starcounter.ThreadHelper.ReleaseYieldBlock();
            }
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
