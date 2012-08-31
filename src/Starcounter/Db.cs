
using Starcounter.Internal;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Starcounter
{

    public static class Db
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
                            ushort tableId = definitionInfo.TableID;
                            ColumnDef[] columns = new ColumnDef[definitionInfo.NumAttributes];
                            for (ushort i = 0; i < columns.Length; i++)
                            {
                                sccoredb.Mdb_AttributeInfo attributeInfo;
                                b = sccoredb.Mdb_DefinitionAttributeIndexToInfo(definition_addr, i, out attributeInfo);
                                if (b != 0)
                                {
                                    columns[i] = new ColumnDef(
                                        new string(attributeInfo.PtrName),
                                        attributeInfo.Type,
                                        (attributeInfo.Flags & sccoredb.MDB_ATTRFLAG_NULLABLE) != 0
                                        );
                                }
                                else
                                {
                                    throw sccoreerr.TranslateErrorCode(sccoredb.Mdb_GetLastError());
                                }
                            }
                            return new TableDef(name, tableId, columns);
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                throw sccoreerr.TranslateErrorCode(sccoredb.Mdb_GetLastError());
            }
        }

        public static void CreateTable(TableDef tableDef)
        {
            unsafe
            {
                ColumnDef[] columns = tableDef.Columns;
                sccoredb.SC_COLUMN_DEFINITION[] column_definitions = new sccoredb.SC_COLUMN_DEFINITION[columns.Length + 1];
                try
                {
                    for (int i = 0; i < columns.Length; i++)
                    {
                        column_definitions[i].name = (byte*)Marshal.StringToCoTaskMemAnsi(columns[i].Name);
                        column_definitions[i].type = columns[i].Type;
                        column_definitions[i].is_nullable = columns[i].IsNullable ? (byte)1 : (byte)0;
                    }
                    fixed (byte* fixed_name = Encoding.ASCII.GetBytes(tableDef.Name))
                    {
                        fixed (sccoredb.SC_COLUMN_DEFINITION* fixed_column_definitions = column_definitions)
                        {
                            uint e = sccoredb.sc_create_table(fixed_name, sccoredb.INVALID_DEFINITION_ADDR, fixed_column_definitions);
                            if (e != 0) throw sccoreerr.TranslateErrorCode(e);
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
            throw sccoreerr.TranslateErrorCode(e);
        }

        public static void DropTable(ushort tableId)
        {
            uint e = sccoredb.sc_drop_table(tableId);
            if (e == 0) return;
            throw sccoreerr.TranslateErrorCode(e);
        }

        public static void Transaction(Action action)
        {
            uint e;
            ulong transaction_id;
            ulong handle;
            ulong verify;

            e = sccoredb.sccoredb_create_transaction_and_set_current(0, out transaction_id, out handle, out verify);
            if (e != 0) throw sccoreerr.TranslateErrorCode(e);
            try
            {
                action();

                ulong hiter;
                ulong viter;
                e = sccoredb.sccoredb_begin_commit(out hiter, out viter);
                if (e != 0) throw sccoreerr.TranslateErrorCode(e);

                // TODO: Handle triggers.

                e = sccoredb.sccoredb_complete_commit(1, out transaction_id);
                if (e != 0) throw sccoreerr.TranslateErrorCode(e);
            }
            catch (Exception ex)
            {
                sccoredb.sccoredb_free_transaction(handle, verify);
                throw ex;
            }
        }
    }
}
