﻿// ***********************************************************************
// <copyright file="_QueryModule.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Query.Optimization;
using Starcounter.Query.Sql;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;
using Starcounter.Internal;

namespace Starcounter.Query
{
    /// <summary>
    /// Configuration, initiation and termination of query module.
    /// </summary>
    public static class QueryModule
    {
        // Configuration of query module.
        //static String processFolder = StarcounterEnvironment.SystemDirectory + "\\32BitComponents\\";
        internal static String ProcessFolder = AppDomain.CurrentDomain.BaseDirectory + "32BitComponents\\";
        internal const String ProcessFileName = StarcounterConstants.ProgramNames.ScSqlParser + ".exe";
        internal const String ProcessVersion = "121017";
        internal static Int32 ProcessPort = 0;
        //static readonly String schemaFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\schema.pl";
        internal static String SchemaFolder = AppDomain.CurrentDomain.BaseDirectory + "32BitComponents\\";
        internal const Int32 MaxQueryLength = 3000;
        internal const Int32 MaxQueryRetries = 10;
        internal const Int32 MaxVerifyRetries = 100;
        internal const Int32 TimeBetweenVerifyRetries = 100; // [ms]

        /// <summary>
        /// Initiates query module. Called during start-up.
        /// </summary>
        /// <param name="processPort">External SQL process port. If 0 then default should be used.</param>
        public static void Initiate(Int32 processPort)
        {
#if false
            // Connect managed and native Sql functions.
            UInt32 errCode = SqlConnectivity.InitSqlFunctions();

            // Checking for error code and translating it.
            if (errCode != 0)
                SqlConnectivity.ThrowConvertedServerError(errCode);
#endif

            // Start external SQL process (Prolog-process).
            ProcessPort = processPort;
            if (ProcessPort == 0)
                ProcessPort = StarcounterEnvironment.DefaultPorts.SQLProlog;
            Int32 tickCount = Environment.TickCount;
            PrologManager.Initiate();
            tickCount = Environment.TickCount - tickCount;
        }

        /// <summary>
        /// Removes all schema information from external SQL process (Prolog-process).
        /// </summary>
        internal static void Reset()
        {
            Int32 tickCount = Environment.TickCount;
            Starcounter.ThreadHelper.SetYieldBlock();
            try
            {
                Scheduler scheduler = Scheduler.GetInstance(true);
                PrologManager.DeleteAllSchemaInfo(scheduler);
            }
            finally
            {
                Starcounter.ThreadHelper.ReleaseYieldBlock();
            }
            tickCount = Environment.TickCount - tickCount;
        }

        /// <summary>
        /// </summary>
        /// <param name="typeDefArray"></param>
        public static void UpdateSchemaInfo(TypeDef[] typeDefArray)
        {
            Starcounter.ThreadHelper.SetYieldBlock();
            try
            {
                Scheduler scheduler = Scheduler.GetInstance(true);
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
