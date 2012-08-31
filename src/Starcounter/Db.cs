
using Starcounter.Internal;
using System;
using System.Text;

namespace Starcounter
{

    public static class Db
    {

        public static TableDef LookupTable(string name)
        {
            int b;
            ulong definition_addr;

            b = sccoredb.Mdb_DefinitionFromCodeClassString(name, out definition_addr);
            if (b != 0)
            {
                if (definition_addr != sccoredb.INVALID_DEFINITION_ADDR)
                {
                    sccoredb.Mdb_DefinitionInfo definition_info;
                    b = sccoredb.Mdb_DefinitionToDefinitionInfo(definition_addr, out definition_info);
                    if (b != 0)
                    {
                        return new TableDef(name, definition_info.TableID);
                    }
                }
                else
                {
                    return null;
                }
            }
            throw sccoreerr.TranslateErrorCode(sccoredb.Mdb_GetLastError());
        }

        public static void CreateTable(string name)
        {
            unsafe
            {
                fixed (byte *ascii_name = Encoding.ASCII.GetBytes(name))
                {
                    sccoredb.SC_COLUMN_DEFINITION *column_definitions = stackalloc sccoredb.SC_COLUMN_DEFINITION [1];
                    column_definitions->type = 0;
                    uint e = sccoredb.sc_create_table(ascii_name, sccoredb.INVALID_DEFINITION_ADDR, column_definitions);
                    if (e != 0) throw sccoreerr.TranslateErrorCode(e);
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
