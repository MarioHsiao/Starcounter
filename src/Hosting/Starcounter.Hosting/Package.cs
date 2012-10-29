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

namespace Starcounter.Hosting {

    /// <summary>
    /// Class Package
    /// </summary>
    public class Package {

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

                ExecuteEntryPoint();
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
                Db.CreateIndex(
                    storedTableDef.DefinitionAddr,
                    "auto",
                    0
                    );
            }
#endif

            return storedTableDef;
        }

        /// <summary>
        /// Executes the entry point.
        /// </summary>
        private void ExecuteEntryPoint() {
            if (assembly_ != null) {
                var arguments = this.EntrypointArguments ?? new string[] { };
                assembly_.EntryPoint.Invoke(null, new object[] { arguments });
                OnEntryPointExecuted();
            }
        }

        private void OnProcessingStarted() { OutputElapsedTime("Processing started"); }
        private void OnDatabaseSchemaCheckedAndUpdated() { OutputElapsedTime("Database schema checked and updated"); }
        private void OnTypeDefsRegistered() { OutputElapsedTime("Type definitions registered"); }
        private void OnQueryModuleSchemaInfoUpdated() { OutputElapsedTime("Query module schema information updated"); }
        private void OnEntryPointExecuted() { OutputElapsedTime("Entry point executed"); }
        private void OnProcessingCompleted() { OutputElapsedTime("Processing completed"); }

        private void OutputElapsedTime(string tag)
        {
            long elapsedTicks = stopwatch_.ElapsedTicks;
            Console.WriteLine(string.Concat(elapsedTicks / 10000, ".", elapsedTicks % 10000, ":", tag));
        }
    }
}
