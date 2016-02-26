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

        public void Apply(TransactionData transaction_data)
        {
            foreach (var c in transaction_data.creates)
            {
                var table_def = LookupTable(c.table);
                if (table_def==null)
                    throw ErrorCode.ToException(Error.SCERRTABLENOTFOUND, string.Format("Table: {0}", c.table));

                ulong object_ref;
                DbState.InsertWithId(table_def.TableId, c.key.object_id, out object_ref);

                fill_record(c.key.object_id, object_ref, table_def, c.columns);
            }

            foreach (var u in transaction_data.updates)
            {
                var table_def = LookupTable(u.table);
                if (table_def == null)
                    throw ErrorCode.ToException(Error.SCERRTABLENOTFOUND, string.Format("Table: {0}", u.table));

                ObjectRef? o = DbState.Lookup(u.key.object_id);
                if (!o.HasValue)
                    throw ErrorCode.ToException(Error.SCERRRECORDNOTFOUND, string.Format("ObjectID: {0}", u.key.object_id));

                fill_record(u.key.object_id, o.Value.ETI, table_def, u.columns);
            }

            foreach( var d in transaction_data.deletes)
            {
                ObjectRef? o = DbState.Lookup(d.key.object_id);
                if (!o.HasValue)
                    throw ErrorCode.ToException(Error.SCERRRECORDNOTFOUND, string.Format("ObjectID: {0}", d.key.object_id));

                Db.Delete(o.Value);
            }

        }

        static private void fill_record(ulong object_id, ulong object_ref, TableDef table_def, column_update[] columns)
        {
            foreach (var c in columns)
            {
                var column_def = table_def.ColumnDefs.Select((cd, i) => new { cd = cd, i = i }).Where(d => d.cd.Name == c.name).SingleOrDefault();
                if (column_def == null)
                    throw ErrorCode.ToException(Error.SCERRCOLUMNNOTFOUND, string.Format("Table.Column: {0}.{1}", table_def.Name, c.name));

                put_value(object_id, object_ref, column_def.cd.Type, column_def.i, c.value);
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
                    DbState.WriteObjectRaw(object_id, object_ref, column_index, new ObjectRef { ObjectID = ((reference)value).object_id, ETI = 0 });
                    break;

                default:
                    throw new System.ArgumentException();
            }
        }
    }
}
