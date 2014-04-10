using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Starcounter {
    public static class Reload {
        public static string QuoteName(string name) {
            return "\"" + name + "\"";
        }
        internal static int Unload(string fileName) {
            int totalNrObj = 0;
            // Create empty file
            using (StreamWriter fileStream = new StreamWriter(fileName, false)) {
                fileStream.WriteLine("Database dump. DO NOT EDIT!");
            }
#if DEBUG
            unsafe {
                sccoredb.SCCOREDB_TABLE_INFO tinfo;
                sccoredb.sccoredb_get_table_info_by_name("RawView", out tinfo);
                sccoredb.SCCOREDB_COLUMN_INFO cinfo;
                sccoredb.sccoredb_get_column_info(tinfo.table_id, 5, out cinfo);
                Debug.Assert(new String(cinfo.name) == "FullName");
            }
#endif // DEBUG
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                Debug.Assert(!String.IsNullOrEmpty(tbl.FullName));
                int tblNrObj = 0;
                StringBuilder inStmt = new StringBuilder();
                StringBuilder selectObjs = new StringBuilder();
                inStmt.Append("INSERT INTO ");
                inStmt.Append(QuoteName(tbl.FullName));
                inStmt.Append("(__id");
                selectObjs.Append("SELECT __o as __id");
                foreach (TableColumn col in Db.SQL<TableColumn>("select c from tablecolumn c where basetable = ?", tbl)) {
                    inStmt.Append(",");
                    inStmt.Append(QuoteName(col.Name));
                    selectObjs.Append(",");
                    selectObjs.Append(QuoteName(col.Name));
                }
                inStmt.Append(")");
                selectObjs.Append(" FROM ");
                selectObjs.Append(QuoteName(tbl.MaterializedTable.Name));
                selectObjs.Append(" __o");
                SqlEnumerator<IObjectView> selectEnum = (SqlEnumerator<IObjectView>)Db.SQL<IObjectView>(selectObjs.ToString()).GetEnumerator();
                Debug.Assert(selectEnum.PropertyBinding == null);
                Debug.Assert(selectEnum.TypeBinding != null);
                Debug.Assert(selectEnum.TypeBinding.PropertyCount > 0);
                inStmt.Append("VALUES");
                while (selectEnum.MoveNext()) {
                    IObjectView val = selectEnum.Current;
                    inStmt.Append("(");
                    Debug.Assert(selectEnum.TypeBinding.GetPropertyBinding(0).TypeCode == DbTypeCode.Object);
                    inStmt.Append(val.GetObject(0).GetObjectNo()); // Value __id
                    for (int i = 1; i < selectEnum.TypeBinding.PropertyCount; i++) {
                        inStmt.Append(",");
                        inStmt.Append(GetString(val, i));
                    }
                    inStmt.Append(")");
                    tblNrObj++;
                }
                if (tblNrObj > 0)
                    using (StreamWriter file = new StreamWriter(fileName, true)) {
                        file.WriteLine(inStmt.ToString());
                    }
                totalNrObj += tblNrObj;
            }
            return totalNrObj;
        }

        internal static int Load(string filename) {
            int totalNrObj = 0;
            return totalNrObj;
        }

        public static string GetString(IObjectView values, int index) {
            string nullStr = "NULL";
            DbTypeCode typeCode = values.TypeBinding.GetPropertyBinding(index).TypeCode;
            switch (typeCode) {
                case DbTypeCode.Binary:
                    Binary? binaryVal = values.GetBinary(index);
                    if (binaryVal == null)
                        return nullStr;
                    return binaryVal.ToString();
                case DbTypeCode.LargeBinary:
                    throw ErrorCode.ToException(Error.SCERRNOTSUPPORTED,
                        "Large binary is not supported in unload.");
                case DbTypeCode.Boolean: 
                    Boolean? boolVal = values.GetBoolean(index);
                    if (boolVal == null)
                        return nullStr;
                    return boolVal.ToString();
                case DbTypeCode.DateTime: 
                    DateTime? timeVal = values.GetDateTime(index);
                    if (timeVal == null)
                        return nullStr;
                    return ((DateTime)timeVal).Ticks.ToString();
                case DbTypeCode.Decimal:
                    Decimal? decVal = values.GetDecimal(index);
                    if (decVal == null)
                        return nullStr;
                    return decVal.ToString();
                case DbTypeCode.Single: 
                case DbTypeCode.Double:
                    Double? doubVal = values.GetDouble(index);
                    if (doubVal == null)
                        return nullStr;
                    return doubVal.ToString();
                case DbTypeCode.SByte: 
                case DbTypeCode.Int16: 
                case DbTypeCode.Int32: 
                case DbTypeCode.Int64: 
                    Int64? intVal = values.GetInt64(index);
                    if (intVal == null)
                        return nullStr;
                    return intVal.ToString();
                case DbTypeCode.Object: 
                    Object objVal = values.GetObject(index);
                    if (objVal == null)
                        return nullStr;
                    return objVal.ToString();
                case DbTypeCode.String:
                    String strVal = values.GetString(index);
                    if (strVal == null)
                        return nullStr;
                    return QuoteName(strVal);
                case DbTypeCode.Byte: 
                case DbTypeCode.UInt16: 
                case DbTypeCode.UInt32: 
                case DbTypeCode.UInt64:
                    UInt64? uintVal = values.GetUInt64(index);
                    if (uintVal == null)
                        return nullStr;
                return uintVal.ToString();
            }
            throw ErrorCode.ToException(Error.SCERRUNEXPECTEDINTERNALERROR,
                "Error during unloading a database: type code of selected property is unexpected, " +
                typeCode.ToString() + ".");
        }
    }
}
