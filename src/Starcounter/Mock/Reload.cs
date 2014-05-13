﻿using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Query.Execution;
using System;
using System.Diagnostics;
using System.Globalization;
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

        private static string GetPropertyName(TableColumn col) {
            Debug.Assert(col.BaseTable is RawView);
            PropertyDef prop = (from propDef in Bindings.GetTypeDef(((RawView)col.BaseTable).MaterializedTable.Name).PropertyDefs
                                where propDef.ColumnName == col.MaterializedColumn.Name
                                select propDef).First();
            return prop.Name;
        }

        internal static int Unload(string fileName) {
            int totalNrObj = 0;
            // Create empty file
            using (StreamWriter fileStream = new StreamWriter(fileName, false)) {
                fileStream.WriteLine("Database dump. DO NOT EDIT!");
            }
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                Debug.Assert(!String.IsNullOrEmpty(tbl.FullName));
                int tblNrObj = 0;
                String insertHeader;
                StringBuilder inStmt = new StringBuilder();
                StringBuilder selectObjs = new StringBuilder();
                inStmt.Append("INSERT INTO ");
                inStmt.Append(QuotePath(tbl.FullName));
                inStmt.Append("(__id");
                selectObjs.Append("SELECT __o as __id");
                foreach (TableColumn col in Db.SQL<TableColumn>("select c from tablecolumn c where basetable = ?", tbl)) {
                    inStmt.Append(",");
                    inStmt.Append(QuoteName(col.Name));
                    selectObjs.Append(",");
                    selectObjs.Append(QuoteName(GetPropertyName(col)));
                }
                inStmt.Append(")");
                inStmt.Append("VALUES");
                insertHeader = inStmt.ToString();
                selectObjs.Append(" FROM ");
                selectObjs.Append(QuotePath(tbl.MaterializedTable.Name));
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
                    inStmt.Append("object " + val.GetObject(0).GetObjectNo()); // Value __id
                    for (int i = 1; i < selectEnum.TypeBinding.PropertyCount; i++) {
                        inStmt.Append(",");
                        inStmt.Append(GetString(val, i));
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
            return totalNrObj;
        }

        internal static int Load(string filename) {
            int nrObjs = 0;
            using (StreamReader file = new StreamReader(filename)) {
                string stmt = file.ReadLine();
                if (stmt != "Database dump. DO NOT EDIT!")
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                while ((stmt = file.ReadLine()) != null)
                    Db.SystemTransaction(delegate {
                        nrObjs += Db.Update(stmt);
                    });
            }
            return nrObjs;
        }

        internal static void DeleteAll() {
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                Db.Transaction(delegate {
                    Db.SlowSQL("DELETE FROM " + QuoteName(tbl.MaterializedTable.Name));
                });
            }
        }

        public static string GetString(IObjectView values, int index) {
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
                    return ((Decimal)decVal).ToString(CultureInfo.InvariantCulture);
                case DbTypeCode.Single: 
                case DbTypeCode.Double:
                    Double? doubVal = values.GetDouble(index);
                    if (doubVal == null)
                        return nullStr;
                    return ((Double)doubVal).ToString(CultureInfo.InvariantCulture);
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
                    return "Object " + objVal.GetObjectNo().ToString();
                case DbTypeCode.String:
                    String strVal = values.GetString(index);
                    if (strVal == null)
                        return nullStr;
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
