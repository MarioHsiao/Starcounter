
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
                char* name = null;
                try
                {
                    for (int cc = column_definitions.Length - 1, ci = inheritedColumnCount, di = 0; di < cc; ci++, di++)
                    {
                        column_definitions[di].name = (char *)Marshal.StringToCoTaskMemUni(columns[ci].Name);
                        column_definitions[di].type = BindingHelper.ConvertDbTypeCodeToScTypeCode(columns[ci].Type);
                        column_definitions[di].is_nullable = columns[ci].IsNullable ? (byte)1 : (byte)0;
                    }
                    name = (char*)Marshal.StringToCoTaskMemUni(tableDef.Name);
                    fixed (sccoredb.SC_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                    {
                        uint e = sccoredb.sccoredb_create_table(name, inheritedDefinitionAddr, fixed_column_definitions);
                        if (e != 0) throw ErrorCode.ToException(e);
                    }
                }
                finally
                {
                    if (name != null) Marshal.FreeCoTaskMem((IntPtr)name);
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
            uint e = sccoredb.sccoredb_rename_table(tableId, newName);
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
                uint e = sccoredb.sccoredb_create_index(definitionAddr, name, 0, column_indexes, 0);
                if (e == 0) return;
                throw ErrorCode.ToException(e);
            }
        }

        public static void Transaction(Action action)
        {
            uint maxRetries;
            uint retries;
            uint r;
            ulong handle;
            ulong verify;

            // TODO:
            // Handle if transaction not locked on thread (and not the default
            // transaction) by simply lock the transaction on thread instead of
            // creating a new one.

            maxRetries = 100;
            retries = 0;

            for (; ; )
            {
                r = sccoredb.sccoredb_create_transaction_and_set_current(1, out handle, out verify);
                if (r == 0)
                {
                    try
                    {
                        action();
                        Starcounter.Transaction.Commit(1, 1);
                        return;
                    }
                    catch (Exception ex)
                    {
                        if (
                            sccoredb.sccoredb_set_current_transaction(1, 0, 0) == 0 &&
                            sccoredb.sccoredb_free_transaction(handle, verify) == 0
                            )
                        {
                            if (ex is ITransactionConflictException)
                            {
                                if (++retries <= maxRetries) continue;
                                throw ErrorCode.ToException(Error.SCERRUNHANDLEDTRANSACTCONFLICT, ex);
                            }
                            throw;
                        }
                        HandleFatalErrorInTransactionScope();
                    }
                }

                if (r == Error.SCERRTRANSACTIONLOCKEDONTHREAD)
                {
                    // We already have a transaction locked on thread so we're
                    // already in a transaction scope (possibly an implicit one if
                    // for example in the context of a trigger): Just invoke the
                    // callback and exit.

                    action();
                    return;
                }

                throw ErrorCode.ToException(r);
            }
        }

        private static void HandleFatalErrorInTransactionScope()
        {
            uint e = sccoredb.Mdb_GetLastError();
            ExceptionManager.HandleInternalFatalError(e);
        }
    }
}
