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

        /// <summary>
        /// Initializes package with global settings.
        /// </summary>
        /// <param name="initInternalHttpHandlers">Initializes internal HTTP handlers.</param>
        public static void InitPackage(Action initInternalHttpHandlers)
        {
            InitInternalHttpHandlers_ = initInternalHttpHandlers;
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

        private readonly Application application_;

        private readonly Stopwatch stopwatch_;

        private readonly bool execEntryPointSynchronously_;

        /// <summary>
        /// The processed event_
        /// </summary>
        private readonly ManualResetEvent processedEvent_;
        private volatile uint processedResult;

        /// <summary>
        /// Initialize a simple package, not representing a user code
        /// application, but rather just a set of types to register.
        /// </summary>
        /// <param name="unregisteredTypeDefs">Set of unregistered type definitions.</param>
        /// <param name="stopwatch">A watch used to time package loading.</param>
        internal Package(TypeDef[] unregisteredTypeDefs, Stopwatch stopwatch) {
            unregisteredTypeDefs_ = unregisteredTypeDefs;
            stopwatch_ = stopwatch;
            processedEvent_ = new ManualResetEvent(false);
            processedResult = uint.MaxValue;
            assembly_ = null;
            application_ = null;
        }

		/// <summary>
        /// Initializes a new instance of the <see cref="Package" /> class.
        /// </summary>
        /// <param name="unregisteredTypeDefs">Set of unregistered type definitions.</param>
        /// <param name="stopwatch">A watch used to time package loading.</param>
        /// <param name="assembly">The assembly that comprise the primary
        /// application code.</param>
        /// <param name="application">The application that is being launched.</param>
        /// <param name="execEntryPointSynchronously">
        /// If true the event for processing complete will be set after the entrypoint returns, 
        /// if set to false the event will be set before the entrypoint executes.
        /// </param>
        internal Package(
            TypeDef[] unregisteredTypeDefs, Stopwatch stopwatch, Assembly assembly, Application application, bool execEntryPointSynchronously) 
            : this(unregisteredTypeDefs, stopwatch) {
            assembly_ = assembly;
            application_ = application;
            execEntryPointSynchronously_ = execEntryPointSynchronously;
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        internal void Process()
        {
            var application = application_;
            if (application != null) {
                Application.Index(application);
            }

            try
            {
                OnProcessingStarted();

                UpdateDatabaseSchemaAndRegisterTypes();

				if (application != null && !StarcounterEnvironment.IsAdministratorApp)
					AppsBootstrapper.Bootstrap(application.WorkingDirectory); 

                // Initializing package for all executables.
                if ((InitInternalHttpHandlers_ != null) && (!packageInitialized_))
                {
                    // Registering internal HTTP handlers.
                    InitInternalHttpHandlers_();

                    // Indicating that package is now initialized.
                    packageInitialized_ = true;

                    OnInternalHandlersRegistered();
                }

                // Starting user Main() here.
                if (application != null && execEntryPointSynchronously_)
                    ExecuteEntryPoint(application);

            } catch (Exception e) {
                uint code = 0;
                if (!ErrorCode.TryGetCode(e, out code)) {
                    code = Error.SCERRUNSPECIFIED;
                }
                processedResult = code;
                throw;
            } finally {
                if (processedResult == uint.MaxValue) {
                    processedResult = 0;
                }

                OnProcessingCompleted();
                processedEvent_.Set();
            }

            if (application != null && !execEntryPointSynchronously_)
                ExecuteEntryPoint(application);
        }

        /// <summary>
        /// Waits the until processed.
        /// </summary>
        public uint WaitUntilProcessed() {
            processedEvent_.WaitOne();
            return processedResult;
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

                    LoaderHelper.MapPropertyDefsToColumnDefs(
                        tableDef.ColumnDefs, typeDef.PropertyDefs, out typeDef.ColumnRuntimeTypes
                        );
                }

                OnDatabaseSchemaCheckedAndUpdated();

                Bindings.RegisterTypeDefs(typeDefs);

                OnTypeDefsRegistered();

                QueryModule.UpdateSchemaInfo(typeDefs);

                OnQueryModuleSchemaInfoUpdated();

                if (typeDefs[0].Name == "Starcounter.Metadata.MaterializedTable")
                    Db.Transaction(delegate {
                        Starcounter.SqlProcessor.SqlProcessor.PopulateRuntimeMetadata();
                    });

                OnRuntimeMetadataPopulated();
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

            return storedTableDef;
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint(Application application) {
            var entrypoint = assembly_.EntryPoint;

            try {
                Application.CurrentAssigned = application;
                if (entrypoint.GetParameters().Length == 0) {
                    entrypoint.Invoke(null, null);
                } else {
                    var arguments = application.Arguments;
                    entrypoint.Invoke(null, new object[] { arguments });
                }
            } catch (TargetInvocationException te) {
                var entrypointException = te.InnerException;
                if (entrypointException == null) throw;

                var detail = entrypointException.Message;
                if (!ErrorCode.IsFromErrorCode(entrypointException)) {
                    detail = entrypointException.ToString();
                }

                throw ErrorCode.ToException(Error.SCERRFAILINGENTRYPOINT, te, detail);
            } finally {
                Application.CurrentAssigned = null;
            }

            OnEntryPointExecuted();
        }

        private void OnProcessingStarted() { Trace("Package started."); }
        private void OnInternalHandlersRegistered() { Trace("Internal handlers were registered."); }
        private void OnDatabaseSchemaCheckedAndUpdated() { Trace("Database schema checked and updated."); }
        private void OnTypeDefsRegistered() { Trace("Type definitions registered."); }
        private void OnQueryModuleSchemaInfoUpdated() { Trace("Query module schema information updated."); }
        private void OnEntryPointExecuted() { Trace("Entry point executed."); }
        private void OnProcessingCompleted() { Trace("Processing completed."); }
        private void OnRuntimeMetadataPopulated() { Trace("Runtime meta-data tables were populated with initial data."); }

        [Conditional("TRACE")]
        private void Trace(string message)
        {
            Diagnostics.WriteTrace("loader", stopwatch_.ElapsedTicks, message);

            Diagnostics.WriteTimeStamp("PACKAGE", message);
        }
    }
}
