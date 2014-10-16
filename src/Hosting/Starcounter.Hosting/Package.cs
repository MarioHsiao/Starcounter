// ***********************************************************************
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
using Starcounter.Metadata;
using Starcounter.SqlProcessor;
using System.Collections.Generic;
using StarcounterInternal.Hosting;

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
            Application.CurrentAssigned = application_;
            try {
                ProcessWithinCurrentApplication(application_);
            } finally {
                Application.CurrentAssigned = null;
            }
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

        void ProcessWithinCurrentApplication(Application application) {
            if (application != null) {
                Application.Index(application_);
            }

            try {
                OnProcessingStarted();

                Db.ImplicitScope(() => {
                    UpdateDatabaseSchemaAndRegisterTypes();
                }, 0);

                if (application != null && !StarcounterEnvironment.IsAdministratorApp)
                    AppsBootstrapper.Bootstrap(application.WorkingDirectory);

                // Initializing package for all executables.
                if ((InitInternalHttpHandlers_ != null) && (!packageInitialized_)) {
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
        /// Updates the database schema and register types.
        /// </summary>
        private void UpdateDatabaseSchemaAndRegisterTypes() {
            TypeDef[] typeDefs = unregisteredTypeDefs_;

            if (typeDefs.Length != 0)
            {
                if (typeDefs[0].Name == "Starcounter.Internal.Metadata.Token") {
                    uint e = systables.star_prepare_system_tables();
                    if (e != 0) throw ErrorCode.ToException(e);

                    Starcounter.SqlProcessor.SqlProcessor.PopulateRuntimeMetadata();
                    OnRuntimeMetadataPopulated();
                    // Call CLR class clean up
                    Starcounter.SqlProcessor.SqlProcessor.CleanClrMetadata();
                    OnCleanClrMetadata();

                    ImplicitTransaction.Current(true).SetCurrent();

                    // Populate properties and columns .NET metadata
                    for (int i = 0; i < typeDefs.Length; i++)
                        typeDefs[i].PopulatePropertyDef(typeDefs);
                    OnPopulateMetadataDefs();
                }
                List<TypeDef> updateColumns = new List<TypeDef>();

                for (int i = 0; i < typeDefs.Length; i++)
                {
                    var typeDef = typeDefs[i];
                    var tableDef = typeDef.TableDef;

                    if (CreateOrUpdateDatabaseTable(typeDef))
                        updateColumns.Add(typeDef);

                    // Remap properties representing columns in case the column
                    // order has changed.

                    LoaderHelper.MapPropertyDefsToColumnDefs(
                        tableDef.ColumnDefs, typeDef.PropertyDefs, out typeDef.ColumnRuntimeTypes
                        );
                }
                foreach (TypeDef typeDef in updateColumns)
                    Db.Transaction(delegate {
                        MetadataPopulation.CreateColumnInstances(typeDef);
                    });

#if DEBUG   // Assure that parents were set.
                foreach (TypeDef typeDef in updateColumns) {
                    RawView thisView = Db.SQL<RawView>("select v from rawview v where fullname =?",
                typeDef.TableDef.Name).First;
                    Starcounter.Internal.Metadata.MaterializedTable matTab = Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>(
                        "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
                    Debug.Assert(thisView.MaterializedTable.Equals(matTab));
                    Debug.Assert(matTab != null);
                    RawView parentTab = Db.SQL<RawView>(
                        "select v from rawview v where fullname = ?", typeDef.TableDef.BaseName).First;
                    Debug.Assert(matTab.BaseTable == null && parentTab == null || 
                        matTab.BaseTable != null && parentTab != null &&
                        matTab.BaseTable.Equals(parentTab.MaterializedTable) && thisView.Inherits.Equals(parentTab));
                }
#endif
                OnDatabaseSchemaCheckedAndUpdated();

                Bindings.RegisterTypeDefs(typeDefs);

                OnTypeDefsRegistered();

                QueryModule.UpdateSchemaInfo(typeDefs);

                OnQueryModuleSchemaInfoUpdated();

                // User-level classes are self registering and report in to
                // the installed host manager on first use (via an emitted call
                // in the static class constructor). For system classes, we
                // have to do this by hand.
                if (typeDefs[0].TableDef.Name == "Starcounter.Internal.Metadata.Token") {
                    InitTypeSpecifications();
                    OnTypeSpecificationsInitialized();
                }

                MetadataPopulation.PopulateClrMetadata(typeDefs);
                OnPopulateClrMetadata();
            }
        }

        private void InitTypeSpecifications() {
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.Token.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.MaterializedTable.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.MaterializedColumn.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.MaterializedIndex.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.MaterializedIndexColumn.__starcounterTypeSpecification));

            HostManager.InitTypeSpecification(typeof(Starcounter.Metadata.Type.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Metadata.DbPrimitiveType.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Metadata.MapPrimitiveType.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(ClrPrimitiveType.__starcounterTypeSpecification));

            HostManager.InitTypeSpecification(typeof(Table.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Starcounter.Internal.Metadata.HostMaterializedTable.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(RawView.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(VMView.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(ClrClass.__starcounterTypeSpecification));

            HostManager.InitTypeSpecification(typeof(Member.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(Column.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(CodeProperty.__starcounterTypeSpecification));

            HostManager.InitTypeSpecification(typeof(Index.__starcounterTypeSpecification));
            HostManager.InitTypeSpecification(typeof(IndexedColumn.__starcounterTypeSpecification));
        }

        /// <summary>
        /// Creates the or update database table.
        /// </summary>
        /// <param name="tableDef">The table def.</param>
        /// <returns>TableDef.</returns>
        private bool CreateOrUpdateDatabaseTable(TypeDef typeDef) {
            TableDef tableDef = typeDef.TableDef;
            string tableName = tableDef.Name;
            TableDef storedTableDef = null;
            TableDef pendingUpgradeTableDef = null;
            bool updated = false;

            Db.Transaction(() => {
                storedTableDef = Db.LookupTable(tableName);
                pendingUpgradeTableDef = Db.LookupTable(TableUpgrade.CreatePendingUpdateTableName(tableName));
            });

            if (pendingUpgradeTableDef != null) {
                var continueTableUpgrade = new TableUpgrade(tableName, storedTableDef, pendingUpgradeTableDef);
                storedTableDef = continueTableUpgrade.ContinueEval();
                Db.Transaction(delegate {
                    MetadataPopulation.UpgradeRawTableInstance(typeDef);
                    updated = true;
                });
            }
            //Thread.Sleep(2000);
            if (storedTableDef == null) {
                var tableCreate = new TableCreate(tableDef);
                storedTableDef = tableCreate.Eval();
                Db.Transaction(delegate {
                    MetadataPopulation.CreateRawTableInstance(typeDef);
                    updated = true;
                });
            } else if (!storedTableDef.Equals(tableDef)) {
                var tableUpgrade = new TableUpgrade(tableName, storedTableDef, tableDef);
                storedTableDef = tableUpgrade.Eval();
                Db.Transaction(delegate {
                    MetadataPopulation.UpgradeRawTableInstance(typeDef);
                    updated = true;
                });
            }
            typeDef.TableDef = storedTableDef;

            return updated;
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint(Application application) {
            var entrypoint = assembly_.EntryPoint;

            try {
                if (entrypoint.GetParameters().Length == 0) {

                    Db.ImplicitScope(() => {
                        entrypoint.Invoke(null, null);
                    });

                } else {
                    var arguments = application.Arguments ?? new string[0];

                    Db.ImplicitScope(() => {
                        entrypoint.Invoke(null, new object[] { arguments });
                    });

                }
            } catch (TargetInvocationException te) {
                var entrypointException = te.InnerException;
                if (entrypointException == null) throw;

                var detail = entrypointException.Message;
                if (!ErrorCode.IsFromErrorCode(entrypointException)) {
                    detail = entrypointException.ToString();
                }

                throw ErrorCode.ToException(Error.SCERRFAILINGENTRYPOINT, te, detail);
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
        private void OnTypeSpecificationsInitialized() { Trace("System type specifications initialized."); }
        private void OnRuntimeMetadataPopulated() { Trace("Runtime meta-data tables were created and populated with initial data."); }
        private void OnCleanClrMetadata() { Trace("CLR view meta-data were deleted on host start."); }
        private void OnPopulateClrMetadata() { Trace("CLR view meta-data were populated for the given types."); }
        private void OnPopulateMetadataDefs() { Trace("Properties and columns were populated for the given meta-types."); }

        [Conditional("TRACE")]
        private void Trace(string message)
        {
            Diagnostics.WriteTrace(Loader.Log.Source, stopwatch_.ElapsedTicks, message);
            Diagnostics.WriteTimeStamp(Loader.Log.Source, message);
        }
    }
}
