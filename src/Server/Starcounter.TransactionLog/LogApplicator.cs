using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Binding;
using Starcounter.Internal;
using System.Collections.Concurrent;


namespace Starcounter.TransactionLog
{
    public class LogApplicator : ILogApplicator
    {
        private TableDef LookupTableInBindings(string name)
        {
            return Starcounter.Binding.Bindings.GetTypeDef(name)?.TableDef;
        }

        private ConcurrentDictionary<string, TableDef> m_meta_cache = new ConcurrentDictionary<string, TableDef>();

        private TableDef LookupTableInMetadata(string name)
        {
            return m_meta_cache.GetOrAdd(name, (n) => Db.LookupTable(n));
        }

        private TableDef LookupTable(string name)
        {
            return LookupTableInBindings(name) ?? LookupTableInMetadata(name);
        }

        public void Apply(TransactionData transactionData)
        {
            foreach (var c in transactionData.Creates)
            {
                var table_def = LookupTable(c.Table);
                if (table_def==null)
                    throw ErrorCode.ToException(Error.SCERRTABLENOTFOUND, string.Format("Table: {0}", c.Table));

                ulong object_ref;
                DbState.InsertWithId(table_def.TableId, c.Key.ObjectID, out object_ref);

                fill_record(c.Key.ObjectID, object_ref, table_def, c.Columns);
            }

            foreach (var u in transactionData.Updates)
            {
                var table_def = LookupTable(u.Table);
                if (table_def == null)
                    throw ErrorCode.ToException(Error.SCERRTABLENOTFOUND, string.Format("Table: {0}", u.Table));

                ObjectRef? o = DbState.Lookup(u.Key.ObjectID);
                if (!o.HasValue)
                    throw ErrorCode.ToException(Error.SCERRRECORDNOTFOUND, string.Format("ObjectID: {0}", u.Key.ObjectID));

                fill_record(u.Key.ObjectID, o.Value.ETI, table_def, u.Columns);
            }

            foreach( var d in transactionData.Deletes)
            {
                ObjectRef? o = DbState.Lookup(d.Key.ObjectID);
                if (!o.HasValue)
                    throw ErrorCode.ToException(Error.SCERRRECORDNOTFOUND, string.Format("ObjectID: {0}", d.Key.ObjectID));

                Db.Delete(o.Value);
            }

        }

        static private void fill_record(ulong object_id, ulong object_ref, TableDef table_def, ColumnUpdate[] columns)
        {
            foreach (var c in columns)
            {
                var column_def = table_def.ColumnDefs.Select((cd, i) => new { cd = cd, i = i }).Where(d => d.cd.Name == c.Name).SingleOrDefault();
                if (column_def == null)
                    throw ErrorCode.ToException(Error.SCERRCOLUMNNOTFOUND, string.Format("Table.Column: {0}.{1}", table_def.Name, c.Name));

                put_value(object_id, object_ref, column_def.cd.Type, column_def.i, c.Value);
            }
        }

        static private void put_value(ulong object_id, ulong object_ref, byte column_type, int column_index, object value)
        {
            if (value == null)
            {
                DbState.WriteNull(object_id, object_ref, column_index);
                return;
            }
            switch (column_type)
            {
                case sccoredb.STAR_TYPE_STRING:
                    DbState.WriteString(object_id, object_ref, column_index, (string)value);
                    break;

                case sccoredb.STAR_TYPE_BINARY:
                    DbState.WriteBinary(object_id, object_ref, column_index, new Binary((byte[])value));
                    break;

                case sccoredb.STAR_TYPE_LONG:
                    DbState.WriteInt64(object_id, object_ref, column_index, (long)value);
                    break;

                case sccoredb.STAR_TYPE_ULONG:
                    DbState.WriteUInt64(object_id, object_ref, column_index, (ulong)value);
                    break;

                case sccoredb.STAR_TYPE_DECIMAL:
                    DbState.WriteDecimal(object_id, object_ref, column_index, (decimal)value);
                    break;

                case sccoredb.STAR_TYPE_FLOAT:
                    DbState.WriteSingle(object_id, object_ref, column_index, (float)value);
                    break;

                case sccoredb.STAR_TYPE_DOUBLE:
                    DbState.WriteDouble(object_id, object_ref, column_index, (double)value);
                    break;

                case sccoredb.STAR_TYPE_REFERENCE:
                    DbState.WriteObjectRaw(object_id, object_ref, column_index, new ObjectRef { ObjectID = ((Reference)value).ObjectID, ETI = 0 });
                    break;

                default:
                    throw new System.ArgumentException();
            }
        }
    }
}
