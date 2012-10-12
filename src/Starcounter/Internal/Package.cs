
using Starcounter.Binding;
using Starcounter.Query;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Starcounter.Internal
{
    
    public class Package
    {

        public static void Process(IntPtr hPackage)
        {
            GCHandle gcHandle = (GCHandle)hPackage;
            Package p = (Package)gcHandle.Target;
            gcHandle.Free();
            p.Process();
        }

        private readonly TypeDef[] unregisteredTypeDefs_;
        private readonly Assembly assembly_;
        private readonly ManualResetEvent processedEvent_;

        public Package(
            TypeDef[] unregisteredTypeDefs, // Previously unregistered type definitions.
            Assembly assembly               // Entry point assembly.
            )
        {
            unregisteredTypeDefs_ = unregisteredTypeDefs;
            assembly_ = assembly;
            processedEvent_ = new ManualResetEvent(false);
        }

        internal void Process()
        {
            try
            {
                UpdateDatabaseSchemaAndRegisterTypes();

                ExecuteEntryPoint();
            }
            finally
            {
                processedEvent_.Set();
            }
        }

        public void WaitUntilProcessed()
        {
            processedEvent_.WaitOne();
        }

        public void Dispose()
        {
            processedEvent_.Dispose();
        }

        private void UpdateDatabaseSchemaAndRegisterTypes()
        {
            var typeDefs = unregisteredTypeDefs_;

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

            Bindings.RegisterTypeDefs(typeDefs);

            if (typeDefs.Length != 0)
            {
                QueryModule.UpdateSchemaInfo(typeDefs);
            }
        }

        private TableDef CreateOrUpdateDatabaseTable(TableDef tableDef)
        {
            string tableName = tableDef.Name;
            TableDef storedTableDef = null;
            TableDef pendingUpgradeTableDef = null;

            Db.Transaction(() =>
            {
                storedTableDef = Db.LookupTable(tableName);
                pendingUpgradeTableDef = Db.LookupTable(TableUpgrade.CreatePendingUpdateTableName(tableName));
            });

            if (pendingUpgradeTableDef != null)
            {
                var continueTableUpgrade = new TableUpgrade(tableName, storedTableDef, pendingUpgradeTableDef);
                storedTableDef = continueTableUpgrade.ContinueEval();
            }

            if (storedTableDef == null)
            {
                var tableCreate = new TableCreate(tableDef);
                storedTableDef = tableCreate.Eval();
            }
            else if (!storedTableDef.Equals(tableDef))
            {
                var tableUpgrade = new TableUpgrade(tableName, storedTableDef, tableDef);
                storedTableDef = tableUpgrade.Eval();
            }

#if true
            bool hasIndex = false;
            Db.Transaction(() =>
            {
                hasIndex = storedTableDef.GetAllIndexInfos().Length != 0;
            });
            if (!hasIndex)
            {
                Db.CreateIndex(
                    storedTableDef.DefinitionAddr,
                    "auto",
                    0
                    );
            }
#endif

            return storedTableDef;
        }

        private void ExecuteEntryPoint()
        {
            if (assembly_ != null)
            {
                assembly_.EntryPoint.Invoke(null, new object[] { null });
            }
        }
    }
}
