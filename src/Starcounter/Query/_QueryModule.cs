
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
    const String processVersion = "111208";
    static Int32 processPort;
    static readonly String schemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\schema.pl";
    const Int32 maxQueryLength = 3000;
    const Int32 maxQueryRetries = 10;
    const Int32 maxVerifyRetries = 100;
    const Int32 timeBetweenVerifyRetries = 100; // [ms]

    private static int configuredSQLProcessPort;

    public static void Configure(int sqlProcessPort)
    {
        configuredSQLProcessPort = sqlProcessPort;
    }

    /// <summary>
    /// Initiates query module. Called during start-up.
    /// </summary>
    internal static void Initiate(Boolean newSchema)
    {
        // Connect managed and native Sql functions.
        UInt32 errCode = SqlConnectivity.InitSqlFunctions();

        // Checking for error code and translating it.
        if (errCode != 0)
            SqlConnectivity.ThrowConvertedServerError(errCode);

        // Export database schema and index information, and start Prolog-process.
        processPort = configuredSQLProcessPort;
        if (processPort == 0)
            processPort = StarcounterEnvironment.DefaultPorts.SQLProlog;
        PrologManager.Initiate(newSchema, processFolder, processFileName, processVersion, processPort, schemaFilePath, 
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
