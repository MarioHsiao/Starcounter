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
using Starcounter.Metadata;
using Starcounter.SqlProcessor;
using System.Collections.Generic;
using StarcounterInternal.Hosting;
using Starcounter.Legacy;

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
        /// Exported schema.
        /// </summary>
        static Dictionary<String, Boolean> exportedSchemas_ = new Dictionary<String, Boolean>();

        /// <summary>
        /// Set of type definitions to consider during processing
        /// of this package.
        /// </summary>
        private TypeDef[] typeDefinitions;

        /// <summary>
        /// System type definitions.
        /// </summary>
        static TypeDef[] systemTypeDefinitions_;

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
        /// <param name="typeDefs">Set of type definitions to
        /// consider.</param>
        /// <param name="stopwatch">A watch used to time package loading.</param>
        internal Package(TypeDef[] typeDefs, Stopwatch stopwatch) {
            typeDefinitions = typeDefs;
            stopwatch_ = stopwatch;
            processedEvent_ = new ManualResetEvent(false);
            processedResult = uint.MaxValue;
            assembly_ = null;
            application_ = null;
        }

		/// <summary>
        /// Initializes a new instance of the <see cref="Package" /> class.
        /// </summary>
        /// <param name="typeDefs">Set of type definitions to consider.</param>
        /// <param name="stopwatch">A watch used to time package loading.</param>
        /// <param name="assembly">The assembly that comprise the primary
        /// application code.</param>
        /// <param name="application">The application that is being launched.</param>
        /// <param name="execEntryPointSynchronously">
        /// If true the event for processing complete will be set after the entrypoint returns, 
        /// if set to false the event will be set before the entrypoint executes.
        /// </param>
        internal Package(
            TypeDef[] typeDefs, Stopwatch stopwatch, Assembly assembly, Application application, bool execEntryPointSynchronously) 
            : this(typeDefs, stopwatch) {
            assembly_ = assembly;
            application_ = application;
            execEntryPointSynchronously_ = execEntryPointSynchronously;
        }

        /// <summary>
        /// Processes this instance.
        /// </summary>
        internal void Process()
        {
            // Allocate memory on the stack that can hold a few number of transactions that are fast 
            // to allocate. The pointer to the memory will be kept on the thread. It is important that 
            // TransactionManager.Cleanup() is called before exiting this method since the pointer will 
            // be invalid after.
            unsafe {
                TransactionHandle* shortListPtr = stackalloc TransactionHandle[TransactionManager.ShortListCount];
                TransactionManager.Init(shortListPtr);
            }

            Application.CurrentAssigned = application_;
            try {
                ProcessWithinCurrentApplication(application_);
            } finally {
                Application.CurrentAssigned = null;
                TransactionManager.Cleanup();
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
            var unregisteredTypeDefinitions = GetUnregistered(typeDefinitions);

            if (application != null) {
                Application.Index(application_);
                LegacyContext.Enter(application, typeDefinitions, unregisteredTypeDefinitions);
            }

            try {
                OnProcessingStarted();

                String fullAppId = QueryModule.DatabaseId;

                if (application != null) {
                    fullAppId = application.Name + fullAppId;
                } else {
                    if (typeDefinitions.Length != 0) {
                        systemTypeDefinitions_ = typeDefinitions;
                    }
                }

                // Checking if we already have exported schemas for this app.
                if ((!exportedSchemas_.ContainsKey(fullAppId)) && (systemTypeDefinitions_ != null)) {

                    // Resetting current schema if any.
                    QueryModule.Reset(fullAppId);

                    // Adding system type definitions to this database.
                    if (systemTypeDefinitions_ != typeDefinitions) {
                        QueryModule.UpdateSchemaInfo(fullAppId, systemTypeDefinitions_, false);
                    }
                }

                UpdateDatabaseSchemaAndRegisterTypes(fullAppId, unregisteredTypeDefinitions, typeDefinitions);

                // Checking if there are any type definitions.
                if (typeDefinitions.Length != 0) {

                    // Checking if we already have exported schemas for this app.
                    if (!exportedSchemas_.ContainsKey(fullAppId)) {

                        // Adding user type definitions (+EditionLibraries) to this database.
                        QueryModule.UpdateSchemaInfo(fullAppId, typeDefinitions, false);

                        // Adding this app as processed.
                        exportedSchemas_.Add(fullAppId, true);
                    }
                }

                if ((application != null) && (!StarcounterEnvironment.IsAdministratorApp)) {

                    AppsBootstrapper.Bootstrap(
                        StarcounterEnvironment.Default.UserHttpPort,
                        application.WorkingDirectory,
                        application.Name);
                }

                // Initializing package for all executables.
                if ((InitInternalHttpHandlers_ != null) && (!packageInitialized_)) {

                    // Registering internal HTTP handlers.
                    InitInternalHttpHandlers_();

                    // Indicating that package is now initialized.
                    packageInitialized_ = true;

                    OnInternalHandlersRegistered();
                }

                // The host must always be executed in this context, even if the
                // application start synchronously.
                if (application != null) {
                    ExecuteHost(application);
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

        TypeDef[] GetUnregistered(TypeDef[] all) {
            var typeDefs = all;

            var unregisteredTypeDefs = new List<TypeDef>(typeDefs.Length);
            for (int i = 0; i < typeDefs.Length; i++) {
                var typeDef = typeDefs[i];
                var alreadyRegisteredTypeDef = Bindings.GetTypeDef(typeDef.Name);
                if (alreadyRegisteredTypeDef == null) {
                    unregisteredTypeDefs.Add(typeDef);
                } else {
                    // If the type has a different ASSEMBLY than the already
                    // loaded type, we raise an error. We match by exact version,
                    // i.e including the revision and build.

                    bool assemblyMatch = true;
                    if (!AssemblyName.ReferenceMatchesDefinition(
                        typeDef.TypeLoader.AssemblyName,
                        alreadyRegisteredTypeDef.TypeLoader.AssemblyName)) {
                        assemblyMatch = false;
                    } else if (typeDef.TypeLoader.AssemblyName.Version == null) {
                        assemblyMatch = alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version == null;
                    } else if (alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version == null) {
                        assemblyMatch = false;
                    } else {
                        assemblyMatch = typeDef.TypeLoader.AssemblyName.Version.Equals(
                            alreadyRegisteredTypeDef.TypeLoader.AssemblyName.Version);
                    }

                    if (!assemblyMatch) {
                        throw ErrorCode.ToException(
                            Starcounter.Error.SCERRTYPEALREADYLOADED,
                            string.Format("Type failing: {0}. Already loaded: {1}",
                            typeDef.TypeLoader.ScopedName,
                            alreadyRegisteredTypeDef.TypeLoader.ScopedName));
                    }

                    // A type with the exact matching name has already been loaded
                    // from an assembly with the exact same matching name and the
                    // exact same version. We are still not certain they are completely
                    // equal, but we won't do a full equality-on-value implementation
                    // now. It's for a future release.
                    // TODO:
                    // Provide full checking of type defintion (including table
                    // definition) to see they fully match.
                }
            }

            return unregisteredTypeDefs.ToArray();
        }

        /// <summary>
        /// Updates the database schema and register types.
        /// </summary>
        private void UpdateDatabaseSchemaAndRegisterTypes(String fullAppId, TypeDef[] unregisteredTypeDefs, TypeDef[] allTypeDefs) {

            if (unregisteredTypeDefs.Length != 0) {

                // Using transaction directly here instead of Db.Scope and scope it several times because of 
                // unmanaged functions that creates its own kernel-transaction (and hence resets the current one set).
                using (var transaction = new Transaction(true)) {
                    if (this is StarcounterPackage) {
                            Starcounter.SqlProcessor.SqlProcessor.PopulateRuntimeMetadata(ThreadData.ContextHandle); // TODO EOH:

                            OnRuntimeMetadataPopulated();
                        transaction.Scope(() => {
                            // Call CLR class clean up
                            //Starcounter.SqlProcessor.SqlProcessor.CleanClrMetadata(); // TODO EOH:
                            OnCleanClrMetadata();
                        });
                        transaction.Scope(() => {
                            // Populate properties and columns .NET metadata
                            for (int i = 0; i < unregisteredTypeDefs.Length; i++)
                                unregisteredTypeDefs[i].PopulatePropertyDef(unregisteredTypeDefs);
                            OnPopulateMetadataDefs();
                        });
                    }

                    List<TypeDef> updateColumns = new List<TypeDef>();

                    transaction.Scope(() => {
                        for (int i = 0; i < unregisteredTypeDefs.Length; i++) {
                            var typeDef = unregisteredTypeDefs[i];
                            var tableDef = typeDef.TableDef;

                            if (CreateOrUpdateDatabaseTable(typeDef))
                                updateColumns.Add(typeDef);

                            // Remap properties representing columns in case the column
                            // order has changed.

                            LoaderHelper.MapPropertyDefsToColumnDefs(
                                tableDef.ColumnDefs, typeDef.PropertyDefs, out typeDef.ColumnRuntimeTypes
                                );
                        }
                    });
                    OnTypesCheckedAndUpdated();

                    Bindings.RegisterTypeDefs(unregisteredTypeDefs);

                    OnTypeDefsRegistered();

                    foreach (TypeDef typeDef in updateColumns)
                        Db.SystemTransact(delegate {
                            //MetadataPopulation.CreateColumnInstances(typeDef); // TODO EOH:
                            //MetadataPopulation.UpdateIndexInstances(typeDef.TableDef.TableId);
                        });

#if false // TODO EOH:
#if DEBUG   // Assure that parents were set.
                    transaction.Scope(() => {
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
                    });
#endif
#endif
                    OnColumnsCheckedAndUpdated();

                    // Checking if we already have exported schemas for this app.
                    if (!exportedSchemas_.ContainsKey(fullAppId)) {

                        // Adding user type definitions (+EditionLibraries) to this database.
                        QueryModule.UpdateSchemaInfo(fullAppId, allTypeDefs, false);

                        // Adding this app as processed.
                        exportedSchemas_.Add(fullAppId, true);
                    } 
                    
                    // Checking if we are in app not in plain database.
                    if (fullAppId != QueryModule.DatabaseId) {

                        // Adding new type definitions to database scope with full name generation.
                        QueryModule.UpdateSchemaInfo(QueryModule.DatabaseId, unregisteredTypeDefs, true);
                    }

                    OnQueryModuleSchemaInfoUpdated();

                    InitTypeSpecifications();
                    OnTypeSpecificationsInitialized();

                    //MetadataPopulation.PopulateClrMetadata(unregisteredTypeDefs); // TODO EOH:

                    OnPopulateClrMetadata();
                }
            }
        }

        protected virtual void InitTypeSpecifications() {
            // User-level classes are self registering and report in to
            // the installed host manager on first use (via an emitted call
            // in the static class constructor). Thus, we dont have to take
            // any action.
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

            Db.Transact(() => {
                storedTableDef = Db.LookupTable(tableName);
                pendingUpgradeTableDef = Db.LookupTable(TableUpgrade.CreatePendingUpdateTableName(tableName));
            });

            if (pendingUpgradeTableDef != null) {
                var continueTableUpgrade = new TableUpgrade(tableName, storedTableDef, pendingUpgradeTableDef);
                storedTableDef = continueTableUpgrade.ContinueEval();
                Db.SystemTransact(delegate {
                    MetadataPopulation.UpgradeRawTableInstance(typeDef);
                    updated = true;
                });
            }
            if (storedTableDef == null) {
                var tableCreate = new TableCreate(tableDef);
                storedTableDef = tableCreate.Eval();
#if false // TODO RUS: already populated in meta-layer
                Db.SystemTransact(delegate {
                    MetadataPopulation.CreateRawTableInstance(typeDef); // TODO EOH:
                    updated = true;
                });
#endif
            } else if (!storedTableDef.Equals(tableDef)) {
                var tableUpgrade = new TableUpgrade(tableName, storedTableDef, tableDef);
                storedTableDef = tableUpgrade.Eval();
#if false // TODO RUS: already populated in meta-layer
                Db.SystemTransact(delegate {
                    MetadataPopulation.UpgradeRawTableInstance(typeDef);
                    updated = true;
                });
#endif
            }
            typeDef.TableDef = storedTableDef;

            return updated;
        }

        void ExecuteHost(Application application) {
            Debug.Assert(application != null);
            var entrypoint = assembly_.EntryPoint;
            if (entrypoint != null) {
                var declaringClass = entrypoint.DeclaringType;
                if (typeof(IApplicationHost).IsAssignableFrom(declaringClass)) {
                    using (var codeHost = new CodeHost(application)) {
                        try {
                            var appHost = Activator.CreateInstance(declaringClass) as IApplicationHost;
                            if (appHost == null) {
                                throw ErrorCode.ToException(Error.SCERRINVOKEAPPLICATIONHOST, string.Format(
                                    "Unable to create instance of {0}. Is it public? Does it have a default constructor?", declaringClass.Name));
                            }
                            appHost.HostApplication(codeHost, application);
                        } catch (Exception e) {
                            throw ErrorCode.ToException(Error.SCERRINVOKEAPPLICATIONHOST, e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint(Application application) {
            // No need to keep track of this transaction. It will be cleaned up later.
            if (Db.Environment.HasDatabase)
                TransactionManager.CreateImplicitAndSetCurrent(true);

            var entrypoint = assembly_.EntryPoint;

            try {
                if (entrypoint.GetParameters().Length == 0) {
                    entrypoint.Invoke(null, null);
                } else {
                    var arguments = application.Arguments ?? new string[0];
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
            }

            OnEntryPointExecuted();
        }

        private void OnProcessingStarted() { Trace("Package started."); }
        private void OnInternalHandlersRegistered() { Trace("Internal handlers were registered."); }
        private void OnTypesCheckedAndUpdated() { Trace("Types of database schema checked and updated."); }
        private void OnColumnsCheckedAndUpdated() { Trace("Columns of database schema checked and updated."); }
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
