﻿// ***********************************************************************
// <copyright file="Package.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Query;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;

namespace Starcounter.Hosting {

    /// <summary>
    /// Class Package
    /// </summary>
    public class Package {
        /// <summary>
        /// Initializes internal HTTP handlers.
        /// </summary>
        static Action InitInternalHttpHandlers_ = null;

        /// <summary>
        /// Indicates if package was already initialized for all executables.
        /// </summary>
        static Boolean packageInitialized_ = false;

        private static uint schedulerCount;
        private static byte schedulerNumber;
        private static byte firstAssignableScheduler;
        private static DbSession dbSession;

        /// <summary>
        /// Initializes package with global settings.
        /// </summary>
        /// <param name="initInternalHttpHandlers">Initializes internal HTTP handlers.</param>
        /// <param name="schedulerCount">The numbers of schedulers the database is using.</param>
        public static void InitPackage(Action initInternalHttpHandlers, uint schedulerCount)
        {
            InitInternalHttpHandlers_ = initInternalHttpHandlers;
            Package.schedulerCount = schedulerCount;
            Package.schedulerNumber = 0;

            Package.firstAssignableScheduler = 1;
            if (schedulerCount == 1)
                Package.firstAssignableScheduler = 0;
        }

        /// <summary>
        /// Processes the specified h package.
        /// </summary>
        /// <param name="hPackage">The h package.</param>
        public static void Process(IntPtr hPackage) {
            GCHandle gcHandle = (GCHandle)hPackage;
            Package p = (Package)gcHandle.Target;
            gcHandle.Free();
            p.Process();
        }

        /// <summary>
        /// The unregistered type defs_
        /// </summary>
        private readonly TypeDef[] unregisteredTypeDefs_;

        /// <summary>
        /// The assembly_
        /// </summary>
        private readonly Assembly assembly_;

        private readonly Stopwatch stopwatch_;
        
        /// <summary>
        /// The processed event_
        /// </summary>
        private readonly ManualResetEvent processedEvent_;

        /// <summary>
        /// Gets or sets the logical working directory the entrypoint
        /// assembly runs in.
        /// </summary>
        public string WorkingDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the entrypoint arguments to be given to the
        /// entrypoint when invoked.
        /// </summary>
        public string[] EntrypointArguments {
            get;
            set;
        }

		/// <summary>
        /// Initializes a new instance of the <see cref="Package" /> class.
        /// </summary>
        /// <param name="unregisteredTypeDefs">The unregistered type defs.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="stopwatch"></param>
        public Package(
            TypeDef[] unregisteredTypeDefs, // Previously unregistered type definitions.
            Assembly assembly,              // Entry point assembly.
            Stopwatch stopwatch             // Stopwatch used to measure package load times.
            ) {
            unregisteredTypeDefs_ = unregisteredTypeDefs;
            assembly_ = assembly;
            stopwatch_ = stopwatch;
            processedEvent_ = new ManualResetEvent(false);
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        internal void Process()
        {
            try
            {
                OnProcessingStarted();

                UpdateDatabaseSchemaAndRegisterTypes();

                CallInfrastructureInitializerIfPresent();

                // Initializing package for all executables.
                if ((InitInternalHttpHandlers_ != null) && (!packageInitialized_))
                {
                    // Registering internal HTTP handlers.
                    InitInternalHttpHandlers_();

                    // Indicating that package is now initialized.
                    packageInitialized_ = true;
                }

                // We want the entrypoint to be executed on another scheduler so we can 
                // process more  packages directly and not wait for the usercode to finish.
                ScheduleEntrypointExecution();
            } finally {
                OnProcessingCompleted();
                processedEvent_.Set();
            }
        }

        /// <summary>
        /// Waits the until processed.
        /// </summary>
        public void WaitUntilProcessed() {
            processedEvent_.WaitOne();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose() {
            processedEvent_.Dispose();
        }

        /// <summary>
        /// Updates the database schema and register types.
        /// </summary>
        private void UpdateDatabaseSchemaAndRegisterTypes() {
            var typeDefs = unregisteredTypeDefs_;

            if (typeDefs.Length != 0)
            {
                for (int i = 0; i < typeDefs.Length; i++)
                {
                    var typeDef = typeDefs[i];
                    var tableDef = typeDef.TableDef;

                    tableDef = CreateOrUpdateDatabaseTable(tableDef);
                    typeDef.TableDef = tableDef;

                    // Remap properties representing columns in case the column
                    // order has changed.

                    LoaderHelper.MapPropertyDefsToColumnDefs(tableDef.ColumnDefs, typeDef.PropertyDefs);
                }

                OnDatabaseSchemaCheckedAndUpdated();

                Bindings.RegisterTypeDefs(typeDefs);

                OnTypeDefsRegistered();

                QueryModule.UpdateSchemaInfo(typeDefs);

                OnQueryModuleSchemaInfoUpdated();
            }
        }

        /// <summary>
        /// Creates the or update database table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        /// <returns>TableDef.</returns>
        private TableDef CreateOrUpdateDatabaseTable(TableDef tableDef) {
            string tableName = tableDef.Name;
            TableDef storedTableDef = null;
            TableDef pendingUpgradeTableDef = null;

            Db.Transaction(() => {
                storedTableDef = Db.LookupTable(tableName);
                pendingUpgradeTableDef = Db.LookupTable(TableUpgrade.CreatePendingUpdateTableName(tableName));
            });

            if (pendingUpgradeTableDef != null) {
                var continueTableUpgrade = new TableUpgrade(tableName, storedTableDef, pendingUpgradeTableDef);
                storedTableDef = continueTableUpgrade.ContinueEval();
            }

            if (storedTableDef == null) {
                var tableCreate = new TableCreate(tableDef);
                storedTableDef = tableCreate.Eval();
            } else if (!storedTableDef.Equals(tableDef)) {
                var tableUpgrade = new TableUpgrade(tableName, storedTableDef, tableDef);
                storedTableDef = tableUpgrade.Eval();
            }

#if true
            bool hasIndex = false;
            Db.Transaction(() => {
                hasIndex = storedTableDef.HasIndex();
            });
            if (!hasIndex) {
                short columnIndex = storedTableDef.GetFirstIndexableColumnIndex();
                if (columnIndex != -1) {
                    unsafe {
                        short* column_indexes = stackalloc short[2];
                        column_indexes[0] = columnIndex;
                        column_indexes[1] = -1;
                        var r = sccoredb.sccoredb_create_index(storedTableDef.TableId, "auto", 0, column_indexes, 0);
                        if (r != 0) throw ErrorCode.ToException(r);
                    }
                }
            }
#endif

            return storedTableDef;
        }

        private void CallInfrastructureInitializerIfPresent() {
            if (assembly_ != null && assembly_.EntryPoint != null) {
                var entrypointType = assembly_.EntryPoint.DeclaringType;

                var m = entrypointType.GetMethod("STARCOUNTERGENERATED_InitializeAppsInfrastructure");
                if (m != null) {
                    m.Invoke(null, new object[] { this.WorkingDirectory, this.EntrypointArguments ?? new string[] { } });
                }
            }
        }

        /// <summary>
        /// Schedules the execution of the entrypoint on a separate scheduler from which
        /// we are currently running, to allow long-running entrypoints. 
        /// </summary>
        /// <remarks>
        /// A simple RoundRobin scheme is used to schedule these executions on different
        /// schedulers, and the first scheduler is never used since it it dedicated to 
        /// package processing.
        /// </remarks>
        private void ScheduleEntrypointExecution() {
            // only one package is processed at a time so this function is threadsafe.
            if (dbSession == null)
                dbSession = new DbSession();
            schedulerNumber++;
            if (schedulerNumber >= schedulerCount)
                schedulerNumber = firstAssignableScheduler;
            dbSession.RunAsync(ExecuteEntryPoint, schedulerNumber);
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint() {
            if (assembly_ != null) {
                var entrypoint = assembly_.EntryPoint;
                if (entrypoint.GetParameters().Length == 0) {
                    entrypoint.Invoke(null, null);
                } else {
                    var arguments = this.EntrypointArguments ?? new string[] { };
                    entrypoint.Invoke(null, new object[] { arguments });
                }
                OnEntryPointExecuted();
            }
        }

        private void OnProcessingStarted() { Trace("Processing started."); }
        private void OnDatabaseSchemaCheckedAndUpdated() { Trace("Database schema checked and updated."); }
        private void OnTypeDefsRegistered() { Trace("Type definitions registered."); }
        private void OnQueryModuleSchemaInfoUpdated() { Trace("Query module schema information updated."); }
        private void OnEntryPointExecuted() { Trace("Entry point executed."); }
        private void OnProcessingCompleted() { Trace("Processing completed."); }

        [Conditional("TRACE")]
        private void Trace(string message)
        {
            Diagnostics.WriteTrace("loader", stopwatch_.ElapsedTicks, message);
        }
    }
}
