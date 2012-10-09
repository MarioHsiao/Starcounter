
using Starcounter.Binding;
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Starcounter
{

    public static partial class Db
    {

        public static TableDef LookupTable(string name)
        {
            unsafe
            {
                int b;
                ulong definitionAddr;

                b = sccoredb.Mdb_DefinitionFromCodeClassString(name, out definitionAddr);
                if (b != 0)
                {
                    if (definitionAddr != sccoredb.INVALID_DEFINITION_ADDR)
                    {
                        sccoredb.Mdb_DefinitionInfo definitionInfo;
                        b = sccoredb.Mdb_DefinitionToDefinitionInfo(definitionAddr, out definitionInfo);
                        if (b != 0)
                        {
                            return TableDef.ConstructTableDef(definitionAddr, definitionInfo);
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
            }
        }

        public static void CreateTable(TableDef tableDef)
        {
            CreateTable(tableDef, null);
        }

        public static void CreateTable(TableDef tableDef, TableDef inheritedTableDef)
        {
            unsafe
            {
                int inheritedColumnCount = 0;
                ulong inheritedDefinitionAddr = sccoredb.INVALID_DEFINITION_ADDR;
                if (inheritedTableDef != null)
                {
                    // TODO:
                    // We're assume that the base table definition is complete
                    // (has definition address) and that the current table
                    // definition and the inherited table definition matches.
                    
                    inheritedColumnCount = inheritedTableDef.ColumnDefs.Length;
                    inheritedDefinitionAddr = inheritedTableDef.DefinitionAddr;
                }
                ColumnDef[] columns = tableDef.ColumnDefs;
                sccoredb.SC_COLUMN_DEFINITION[] column_definitions = new sccoredb.SC_COLUMN_DEFINITION[columns.Length - inheritedColumnCount + 1];
                try
                {
                    for (int cc = column_definitions.Length - 1, ci = inheritedColumnCount, di = 0; di < cc; ci++, di++)
                    {
                        column_definitions[di].name = (byte*)Marshal.StringToCoTaskMemAnsi(columns[ci].Name);
                        column_definitions[di].type = BindingHelper.ConvertDbTypeCodeToScTypeCode(columns[ci].Type);
                        column_definitions[di].is_nullable = columns[ci].IsNullable ? (byte)1 : (byte)0;
                    }
                    fixed (byte* fixed_name = Encoding.ASCII.GetBytes(tableDef.Name))
                    {
                        fixed (sccoredb.SC_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                        {
                            uint e = sccoredb.sc_create_table(fixed_name, inheritedDefinitionAddr, fixed_column_definitions);
                            if (e != 0) throw ErrorCode.ToException(e);
                        }
                    }
                }
                finally
                {
                    for (int i = 0; i < column_definitions.Length; i++)
                    {
                        if (column_definitions[i].name != null)
                            Marshal.FreeCoTaskMem((IntPtr)column_definitions[i].name);
                    }
                }
            }
        }

        public static void RenameTable(ushort tableId, string newName)
        {
            uint e = sccoredb.sc_rename_table(tableId, newName);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        public static void DropTable(string name)
        {
            uint e = sccoredb.sccoredb_drop_table(name);
            if (e == 0) return;
            throw ErrorCode.ToException(e);
        }

        public static void CreateIndex(ulong definitionAddr, string name, short columnIndex) // TODO:
        {
            unsafe
            {
                short* column_indexes = stackalloc short[2];
                column_indexes[0] = columnIndex;
                column_indexes[1] = -1;
                uint e = sccoredb.sc_create_index(definitionAddr, name, 0, column_indexes, 0);
                if (e == 0) return;
                throw ErrorCode.ToException(e);
            }
        }

        public static void Transaction(Action action)
        {
            uint r;
            int noBoundary;
            bool completed;
            ulong transaction_id;
            ulong handle;
            ulong verify;

            // TODO:
            // Make boundary check faster. Best approach probably to do a
            // sccoredb_create_transaction_and_set_current that fails if
            // transaction attached.

            r = sccoredb.sccoredb_has_transaction(out noBoundary);
            if (r != 0) goto err;
            
            if (noBoundary == 0)
            {
                completed = false;
                r = sccoredb.sccoredb_create_transaction_and_set_current(0, out transaction_id, out handle, out verify);
                if (r != 0) goto err;
                try
                {
                    Starcounter.Transaction.OnTransactionSwitch();

                    action();

                    ulong hiter;
                    ulong viter;
                    r = sccoredb.sccoredb_begin_commit(out hiter, out viter);
                    if (r != 0) goto err;

                    // TODO: Handle triggers.

                    r = sccoredb.sccoredb_complete_commit(1, out transaction_id);
                    if (r != 0) goto err;

                    completed = true;
                }
                finally
                {
                    if (!completed)
                    {
                        if (
                            sccoredb.Mdb_TransactionSetCurrent(0, 0) == 0 ||
                            sccoredb.sccoredb_free_transaction(handle, verify) != 0
                            )
                        {
                            HandleFatalErrorInTransactionScope();
                        }
                    }
                }
            }
            else
            {
                action();
            }

            return;

        err:
            throw ErrorCode.ToException(r);
        }

        private static void HandleFatalErrorInTransactionScope()
        {
            uint e = sccoredb.Mdb_GetLastError();
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
