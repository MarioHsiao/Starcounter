using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace Starcounter {
    public static class Reload {
        public static string QuoteName(string name) {
            return "\"" + name + "\"";
        }

        public static string QuotePath(string name) {
            int dotPos = -1;
            char dotChar = '.';
            StringBuilder quotedPath = new StringBuilder(name.Length+6);
            while ((dotPos = name.IndexOf(dotChar)) > -1) {
                Debug.Assert(dotPos > 0);
                quotedPath.Append(QuoteName(name.Substring(0, dotPos)));
                quotedPath.Append(dotChar);
                Debug.Assert(name.Length > dotPos + 1);
                name = name.Substring(dotPos + 1);
            }
            Debug.Assert(name.IndexOf(dotChar) == -1);
            quotedPath.Append(QuoteName(name));
            return quotedPath.ToString();
        }

        internal static int Unload(string fileName, ulong shiftId) {
            int totalNrObj = 0;
            // Create empty file
            using (StreamWriter fileStream = new StreamWriter(fileName, false)) {
                fileStream.WriteLine("Database dump. DO NOT EDIT!");
            }
            foreach (materialized_table tbl in Db.SQL<materialized_table>("select t from materialized_table t where table_id > ?", 3)) {
                Debug.Assert(!String.IsNullOrEmpty(tbl.name));
                if (Binding.Bindings.GetTypeDef(tbl.name) == null) {
                    LogSources.Hosting.LogWarning("Table " + tbl.name + " cannot be unloaded, since its class is not loaded.");
                    Console.WriteLine("Warning: Table " + tbl.name + " cannot be unloaded, since its class is not loaded.");
                } else {
                    int tblNrObj = 0;
                    String insertHeader;
                    StringBuilder inStmt = new StringBuilder();
                    StringBuilder selectObjs = new StringBuilder();
                    inStmt.Append("INSERT INTO ");
                    inStmt.Append(QuotePath(tbl.name));
                    inStmt.Append("(__id");
                    selectObjs.Append("SELECT __o as __id");
                    foreach (materialized_column col in Db.SQL<materialized_column>("select c from materialized_column c where \"table\" = ?", tbl)) {
                        if (col.name != "__id") {
                            PropertyDef prop = (from propDef in Bindings.GetTypeDef(tbl.name).PropertyDefs
                                                where propDef.ColumnName == col.name
                                                select propDef).First<PropertyDef>();
                            inStmt.Append(",");
                            inStmt.Append(QuoteName(col.name));
                            selectObjs.Append(",");
                            selectObjs.Append(QuoteName(prop.Name));

                        }
                    }
                    inStmt.Append(")");
                    inStmt.Append("VALUES");
                    insertHeader = inStmt.ToString();
                    selectObjs.Append(" FROM ");
                    selectObjs.Append(QuotePath(tbl.name));
                    selectObjs.Append(" __o");
                    SqlEnumerator<IObjectView> selectEnum = (SqlEnumerator<IObjectView>)Db.SQL<IObjectView>(selectObjs.ToString()).GetEnumerator();
                    Debug.Assert(selectEnum.PropertyBinding == null);
                    Debug.Assert(selectEnum.TypeBinding != null);
                    Debug.Assert(selectEnum.TypeBinding.PropertyCount > 0);
                    while (selectEnum.MoveNext()) {
                        IObjectView val = selectEnum.Current;
                        if (tblNrObj == 0)
                            inStmt.Append("(");
                        else
                            inStmt.Append(",(");
                        Debug.Assert(selectEnum.TypeBinding.GetPropertyBinding(0).TypeCode == DbTypeCode.Object);
                        if (val.GetObject(0).GetType().ToString() != tbl.name) continue;
                        inStmt.Append("object " + (val.GetObject(0).GetObjectNo() + shiftId).ToString()); // Value __id
                        for (int i = 1; i < selectEnum.TypeBinding.PropertyCount; i++) {
                            inStmt.Append(",");
                            inStmt.Append(GetString(val, i, shiftId));
                        }
                        inStmt.Append(")");
                        tblNrObj++;
                        if (tblNrObj == 1000) {
                            using (StreamWriter file = new StreamWriter(fileName, true)) {
                                file.WriteLine(inStmt.ToString());
                            }
                            totalNrObj += tblNrObj;
                            tblNrObj = 0;
                            inStmt = new StringBuilder();
                            inStmt.Append(insertHeader);
                        }
                    }
                    if (tblNrObj > 0)
                        using (StreamWriter file = new StreamWriter(fileName, true)) {
                            file.WriteLine(inStmt.ToString());
                        }
                    totalNrObj += tblNrObj;
                }
            }
            return totalNrObj;
        }

        public static string GetString(IObjectView values, int index, ulong shiftId) {
            string nullStr = "NULL";
            DbTypeCode typeCode = values.TypeBinding.GetPropertyBinding(index).TypeCode;
            switch (typeCode) {
                case DbTypeCode.Binary:
                    Binary? binaryVal = values.GetBinary(index);
                    if (binaryVal == null || ((Binary)binaryVal).IsNull)
                        return nullStr;
                    return "BINARY '" + Db.BinaryToHex((Binary)binaryVal) + "'";
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
                    return "Object " + (objVal.GetObjectNo() + shiftId).ToString();
                case DbTypeCode.String:
                    String strVal = values.GetString(index);
                    if (strVal == null)
                        return nullStr;
                    strVal = strVal.Replace("'", "''");
                    return "'" + strVal + "'";
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
