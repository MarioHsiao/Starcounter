
using Starcounter.Binding;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Starcounter.Internal
{
    
    internal class Package
    {

        internal static void Process(IntPtr hPackage)
        {
            GCHandle gcHandle = (GCHandle)hPackage;
            Package p = (Package)gcHandle.Target;
            gcHandle.Free();
            p.Process();
        }

        private readonly TypeDef[] unregisteredTypeDefs_;
        private readonly Assembly assembly_;
        private readonly ManualResetEvent processedEvent_;

        internal Package(
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

        internal void WaitUntilProcessed()
        {
            processedEvent_.WaitOne();
        }

        internal void Dispose()
        {
            processedEvent_.Dispose();
        }

        private void UpdateDatabaseSchemaAndRegisterTypes()
        {
            var typeDefs = unregisteredTypeDefs_;

            for (int i = 0; i < typeDefs.Length; i++)
            {
                var typeDef = typeDefs[i];
                typeDef.TableDef = CreateOrUpdateDatabaseTable(typeDef.TableDef);
            }

            for (int i = 0; i < typeDefs.Length; i++)
            {
                Bindings.RegisterTypeDef(typeDefs[i]);
            }

            Fix.ResetTheQueryModule();
        }

        private TableDef CreateOrUpdateDatabaseTable(TableDef tableDef)
        {
            // TODO: Handle inheritence.

            TableDef storedTableDef = null;

            Db.Transaction(() =>
            {
                storedTableDef = Db.LookupTable(tableDef.Name);
            });

            if (storedTableDef == null)
            {
                Db.CreateTable(tableDef);

                Db.Transaction(() =>
                {
                    storedTableDef = Db.LookupTable(tableDef.Name);
                });

                // TODO:

                Db.CreateIndex(
                    storedTableDef.DefinitionAddr,
                    string.Concat(storedTableDef.Name, "_AUTO"),
                    1
                    );
            }
            else
            {
                // TODO: Update if different structure.
            }

            return storedTableDef;
        }

        private void ExecuteEntryPoint()
        {
            assembly_.EntryPoint.Invoke(null, new object[] { null });
        }
    }
}
