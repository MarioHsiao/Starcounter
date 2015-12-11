using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Binding;
using Starcounter.Internal;


namespace Starcounter.TransactionLog
{
    public class LogApplicator : ILogApplicator
    {
        public void Apply(TransactionData transaction_data)
        {
            foreach (var c in transaction_data.creates)
            {
                var table_def = Db.LookupTable(c.table);

                ulong object_ref;
                DbState.InsertWithId(table_def.TableId, c.key.object_id, out object_ref);

                fill_record(c.key.object_id, object_ref, table_def, c.columns);
            }

            foreach (var u in transaction_data.updates)
            {
                var table_def = Db.LookupTable(u.table);

                fill_record(u.key.object_id, DbState.Lookup(u.key.object_id).Value.ETI, table_def, u.columns);
            }

            foreach( var d in transaction_data.deletes)
            {
                DbHelper.FromID(d.key.object_id).Delete();
            }

        }

        static private void fill_record(ulong object_id, ulong object_ref, TableDef table_def, column_update[] columns)
        {
            foreach (var c in columns)
            {
                var column_def = table_def.ColumnDefs.Select((cd, i) => new { cd = cd, i = i }).Where(d => d.cd.Name == c.name).SingleOrDefault();
                if (column_def == null)
                    throw ErrorCode.ToException(Error.SCERRSQLUNKNOWNCOLUMN);

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
