
using Starcounter.Binding;
using Starcounter.Bootstrap.Hosting;
using Starcounter.Internal;
using Starcounter.Legacy;
using Starcounter.Metadata;
using Starcounter.Query;
using Starcounter.SqlProcessor;
using StarcounterInternal.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Class Package
    /// </summary>
    internal class Package {
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

        private readonly Application application_;

        private readonly ApplicationDirectory appDirectory_;

        private readonly Stopwatch stopwatch_;

        private EntrypointOptions entrypointOptions;

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
            application_ = null;
            appDirectory_ = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Package" /> class.
        /// </summary>
        /// <param name="typeDefs">Set of type definitions to consider.</param>
        /// <param name="stopwatch">A watch used to time package loading.</param>
        /// <param name="application">The application that is being launched.</param>
        /// <param name="appDir">The materialized application directory.</param>
        /// <param name="entryOptions">How and if to execute an entrypoint</param>
        internal Package(
            TypeDef[] typeDefs, 
            Stopwatch stopwatch, 
            Application application, 
            ApplicationDirectory appDir, 
            EntrypointOptions entryOptions) 
            : this(typeDefs, stopwatch) {

            application_ = application;
            appDirectory_ = appDir;

            entrypointOptions = entryOptions;
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
                ProcessWithinCurrentApplication(application_, appDirectory_);
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
        
        void ProcessWithinCurrentApplication(Application application, ApplicationDirectory applicationDir) {
            //Debugger.Launch();

            Assembly assembly = null;
            if (application == null) {
                entrypointOptions = EntrypointOptions.DontRun;
            }
            else
            {
                assembly = LoadMainAssembly(application, applicationDir);
            }

            var unregisteredTypeDefinitions = GetUnregistered(typeDefinitions);

            if (application != null) {
                Application.Index(application);
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

                        OnPrologSchemaInfoUpdated();
                    }
                }

                if ((application != null) && (!StarcounterEnvironment.IsAdministratorApp)) {

                    var port = StarcounterEnvironment.Default.UserHttpPort;
                    var appDir = application.WorkingDirectory;

                    AppsBootstrapper.Bootstrap(port, appDir, application.Name);

                    foreach (var resourceDir in application.ResourceDirectories) {
                        AppsBootstrapper.AddStaticFileDirectory(resourceDir, port);
                    }

                    OnStaticDirectoriesAdded();
                }

                // Initializing package for all executables.
                if ((InitInternalHttpHandlers_ != null) && (!packageInitialized_)) {

                    // Registering internal HTTP handlers.
                    InitInternalHttpHandlers_();

                    // Indicating that package is now initialized.
                    packageInitialized_ = true;

                    OnInternalHandlersRegistered();
                }

                if (entrypointOptions != EntrypointOptions.DontRun)
                {
                    // The host must always be executed in this context, even if the
                    // application start synchronously.
                    try
                    {
                        ExecuteHost(application, assembly);
                    }
                    finally
                    {
                        LegacyContext.Exit(application);
                    }

                    // Starting user Main() here.
                    if (entrypointOptions == EntrypointOptions.RunSynchronous)
                    {
                        ExecuteEntryPoint(application, assembly);
                    }
                }

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

            if (entrypointOptions == EntrypointOptions.RunAsynchronous)
            {
                ExecuteEntryPoint(application, assembly);
            }
        }

        Assembly LoadMainAssembly(Application application, ApplicationDirectory appDir) {
            var assemblyResolver = Loader.Resolver;

            assemblyResolver.PrivateAssemblies.RegisterApplicationDirectory(appDir);
            OnInputVerifiedAndAssemblyResolverUpdated();

            var assembly = assemblyResolver.ResolveApplication(application.HostedFilePath);
            if (assembly.EntryPoint == null) {
                throw ErrorCode.ToException(
                    Error.SCERRAPPLICATIONNOTANEXECUTABLE, string.Format("Failing application file: {0}", application.HostedFilePath));
            }
            OnTargetAssemblyLoaded();

            return assembly;
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
        protected virtual void UpdateDatabaseSchemaAndRegisterTypes(String fullAppId, TypeDef[] unregisteredTypeDefs, TypeDef[] allTypeDefs) {
            if (unregisteredTypeDefs.Length != 0) {
                List<TypeDef> updateColumns = new List<TypeDef>();
                for (int i = 0; i < unregisteredTypeDefs.Length; i++) {
                    var typeDef = unregisteredTypeDefs[i];

                    if (CreateOrUpdateDatabaseTable(typeDef))
                        updateColumns.Add(typeDef);

                    // Remap properties representing columns in case the column
                    // order has changed.
                    var tableDef = typeDef.TableDef;

                    LoaderHelper.MapPropertyDefsToColumnDefs(
                        tableDef, tableDef.ColumnDefs, typeDef.PropertyDefs, out typeDef.ColumnRuntimeTypes
                        );
                }
                OnTypesCheckedAndUpdated();

                Db.Scope(() => {
                    Bindings.RegisterTypeDefs(unregisteredTypeDefs);
                });
                OnTypeDefsRegistered();

#if DEBUG       // Assure that parents were set.
                Db.Scope(() => {
                    foreach (TypeDef typeDef in updateColumns) {
                        RawView thisView = Db.SQL<RawView>("select v from rawview v where fullname =?",
                    typeDef.TableDef.Name).First;
                        Debug.Assert(thisView != null);
                        RawView parentTab = Db.SQL<RawView>(
                            "select v from rawview v where fullname = ?", typeDef.TableDef.BaseName).First;
                        Debug.Assert(String.IsNullOrEmpty(typeDef.TableDef.BaseName) && parentTab == null ||
                            parentTab != null && thisView.Inherits.Equals(parentTab));
                    }
                }, true);
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

                if(systemTypeDefinitions_ == typeDefinitions) {
                    MetadataPopulation.PopulateClrPrimitives();
                }

                MetadataPopulation.PopulateClrMetadata(unregisteredTypeDefs);

                OnPopulateClrMetadata();
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
        /// <param name="typeDef">The type definition for the table to update or create.</param>
        /// <returns>f.</returns>
        private bool CreateOrUpdateDatabaseTable(TypeDef typeDef) {
            TableDef tableDef = typeDef.TableDef;
            string tableName = tableDef.Name;
            TableDef storedTableDef = null;
            bool updated = false;
            
            Db.Transact(() => {
                storedTableDef = LookupTable(tableName);
            });
            
            if (storedTableDef == null) {
                var tableCreate = new TableCreate(tableDef);
                storedTableDef = tableCreate.Eval();
                updated = true;
            } else if (!storedTableDef.Equals(tableDef)) {
                var tableUpgrade = new TableUpgrade(tableName, storedTableDef, tableDef);
                storedTableDef = tableUpgrade.Eval();
                updated = true;
            }
            typeDef.TableDef = storedTableDef;

            return updated;
        }

        protected virtual TableDef LookupTable(string name) {
            return Db.LookupTable(name);
        }
        
        void ExecuteHost(Application application, Assembly assembly) {
            Debug.Assert(application != null);
            var entrypoint = assembly.EntryPoint;
            if (entrypoint != null) {
                var declaringClass = entrypoint.DeclaringType;
                if (typeof(IApplicationHost).IsAssignableFrom(declaringClass)) {
                    try {
                        var appHost = Activator.CreateInstance(declaringClass) as IApplicationHost;
                        if (appHost == null) {
                            throw ErrorCode.ToException(Error.SCERRINVOKEAPPLICATIONHOST, string.Format(
                                "Unable to create instance of {0}. Is it public? Does it have a default constructor?", declaringClass.Name));
                        }

                        appHost.HostApplication(application);

                    } catch (Exception e) {
                        throw ErrorCode.ToException(Error.SCERRINVOKEAPPLICATIONHOST, e);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint(Application application, Assembly assembly) {
            var transactMain = application.TransactEntrypoint;

            // No need to keep track of this transaction. It will be cleaned up later.
            // @chrhol, does the same apply even if we make it a write transaction?
            // TODO:
            TransactionHandle th = TransactionHandle.Invalid;
            if (Db.Environment.HasDatabase) {
                var readOnly = !transactMain;
                th = TransactionManager.CreateImplicitAndSetCurrent(readOnly);
            }

            // Mapping existing objects if any.
            if (MapConfig.Enabled) {
                StarcounterEnvironment.RunWithinApplication(null, () => {
                    Self.GET("/sc/map");
                });
            }

            var entrypoint = assembly.EntryPoint;

            try {

                if (entrypoint.GetParameters().Length == 0) {
                    entrypoint.Invoke(null, null);
                } else {
                    var arguments = application.Arguments ?? new string[0];
                    entrypoint.Invoke(null, new object[] { arguments });
                }

                // Not sure about this pattern with the new transaction API's,
                // and also not sure how to react to an error in the entrypoint.
                // If one occur, we'll restart the host, but is that enough?
                // Ask @chrhol about this.
                // TODO:

                if (transactMain && th != TransactionHandle.Invalid) {
                    new Transaction(th).Commit();
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
        private void OnStaticDirectoriesAdded() { Trace("Static directories are added."); }
        private void OnPrologSchemaInfoUpdated() { Trace("Prolog exported schema info."); }
        private void OnTypesCheckedAndUpdated() { Trace("Types of database schema checked and updated."); }
        private void OnColumnsCheckedAndUpdated() { Trace("Columns of database schema checked and updated."); }
        private void OnTypeDefsRegistered() { Trace("Type definitions registered."); }
        private void OnQueryModuleSchemaInfoUpdated() { Trace("Query module schema information updated."); }
        private void OnEntryPointExecuted() { Trace("Entry point executed."); }
        private void OnProcessingCompleted() { Trace("Processing completed."); }
        private void OnTypeSpecificationsInitialized() { Trace("System type specifications initialized."); }
        private void OnPopulateClrMetadata() { Trace("CLR view meta-data were populated for the given types."); }
        private void OnPopulateMetadataDefs() { Trace("Properties and columns were populated for the given meta-types."); }
        private void OnTargetAssemblyLoaded() { Trace("Target assembly loaded."); }
        private void OnInputVerifiedAndAssemblyResolverUpdated() { Trace("Input verified and assembly resolver updated."); }

        [Conditional("TRACE")]
        protected void Trace(string message)
        {
            Diagnostics.WriteTrace(Loader.Log.Source, stopwatch_.ElapsedTicks, message);
            Diagnostics.WriteTimeStamp(Loader.Log.Source, message);
        }
    }
}
