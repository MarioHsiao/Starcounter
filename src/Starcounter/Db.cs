
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
                ulong definition_addr;

                b = sccoredb.Mdb_DefinitionFromCodeClassString(name, out definition_addr);
                if (b != 0)
                {
                    if (definition_addr != sccoredb.INVALID_DEFINITION_ADDR)
                    {
                        sccoredb.Mdb_DefinitionInfo definitionInfo;
                        b = sccoredb.Mdb_DefinitionToDefinitionInfo(definition_addr, out definitionInfo);
                        if (b != 0)
                        {
                            ushort tableId = definitionInfo.table_id;
                            uint columnCount = definitionInfo.column_count;
                            string baseName = null;

                            if (definitionInfo.inherited_definition_addr != sccoredb.INVALID_DEFINITION_ADDR)
                            {
                                b = sccoredb.Mdb_DefinitionToDefinitionInfo(definitionInfo.inherited_definition_addr, out definitionInfo);
                                if (b != 0)
                                {
                                    baseName = new String(definitionInfo.table_name);
                                }
                            }

                            if (b != 0)
                            {
                                ColumnDef[] columns = new ColumnDef[columnCount];
                                for (ushort i = 0; i < columns.Length; i++)
                                {
                                    sccoredb.Mdb_AttributeInfo attributeInfo;
                                    b = sccoredb.Mdb_DefinitionAttributeIndexToInfo(definition_addr, i, out attributeInfo);
                                    if (b != 0)
                                    {
                                        columns[i] = new ColumnDef(
                                            new string(attributeInfo.PtrName),
                                            BindingHelper.ConvertScTypeCodeToDbTypeCode(attributeInfo.Type),
                                            (attributeInfo.Flags & sccoredb.MDB_ATTRFLAG_NULLABLE) != 0,
                                            (attributeInfo.Flags & sccoredb.MDB_ATTRFLAG_DERIVED) != 0
                                            );
                                    }
                                    else
                                    {
                                        throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
                                    }
                                }
                                return new TableDef(name, baseName, columns, tableId, definition_addr);
                            }
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

        public static void DropTable(ushort tableId)
        {
            uint e = sccoredb.sc_drop_table(tableId);
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
            uint e;
            ulong transaction_id;
            ulong handle;
            ulong verify;

            e = sccoredb.sccoredb_create_transaction_and_set_current(0, out transaction_id, out handle, out verify);
            if (e != 0) throw ErrorCode.ToException(e);
            try
            {
                action();

                ulong hiter;
                ulong viter;
                e = sccoredb.sccoredb_begin_commit(out hiter, out viter);
                if (e != 0) throw ErrorCode.ToException(e);

                // TODO: Handle triggers.

                e = sccoredb.sccoredb_complete_commit(1, out transaction_id);
                if (e != 0) throw ErrorCode.ToException(e);
            }
            catch (Exception ex)
            {
                sccoredb.sccoredb_free_transaction(handle, verify);
                throw ex;
            }
        }
    }
}
