using Starcounter.Binding;
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

        /// <summary>
        /// Gets the name of a property the unload can use to read the
        /// raw value of <paramref name="col"/> using the high-level SQL
        /// API.
        /// </summary>
        /// <param name="col">The column the unload are creating an
        /// INSERT statement using.</param>
        /// <returns>A property that can be used to read the value of
        /// the given column using the high-level SQL API.</returns>
        private static string GetPropertyName(Column col) {
            Debug.Assert(col.Table is RawView);

            var typeDef = Bindings.GetTypeDef(((RawView)col.Table).FullName);
            var prop = typeDef.PropertyDefs.FirstOrDefault((candidate) => {
                return candidate.ColumnName == col.MaterializedColumn.Name;
            });
            if (prop == null) {
                throw ErrorCode.ToException(
                    Error.SCERRCOLUMNHASNOPROPERTY, 
                    string.Format("Missing property for {0}.{1}", col.Table.Name, col.MaterializedColumn.Name));
            }

            return prop.Name;
        }

        internal static int Unload(string fileName, ulong shiftId, Boolean unloadAll) {
            int totalNrObj = 0;
            // Create empty file
            using (StreamWriter fileStream = new StreamWriter(fileName, false)) {
                fileStream.WriteLine("Database dump. DO NOT EDIT!");
            }
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                Debug.Assert(!String.IsNullOrEmpty(tbl.UniqueIdentifier));
                if (Binding.Bindings.GetTypeDef(tbl.FullName) == null) {
                    if (unloadAll)
                        throw ErrorCode.ToException(Error.SCERRUNLOADTABLENOCLASS, 
                            "Table "+tbl.FullName+" cannot be unloaded.");
                    else
                        LogSources.Unload.LogWarning("Table " + tbl.FullName + " cannot be unloaded, since its class is not loaded.");
                    //Console.WriteLine("Warning: Table " + tbl.FullName + " cannot be unloaded, since its class is not loaded.");
                } else {
                    int tblNrObj = 0;
                    String insertHeader;
                    StringBuilder inStmt = new StringBuilder();
                    StringBuilder selectObjs = new StringBuilder();
                    inStmt.Append("INSERT INTO ");
                    inStmt.Append(QuotePath(tbl.UniqueIdentifier));
                    inStmt.Append("(__id");
                    selectObjs.Append("SELECT __o as __id");
                        foreach (Column col in Db.SQL<Column>("select c from starcounter.metadata.column c where c.table = ?", tbl)) {
                            if (col.Name != "__id") {
                                inStmt.Append(",");
                                inStmt.Append(QuoteName(col.Name));
                                selectObjs.Append(",");
                                selectObjs.Append(QuoteName(GetPropertyName(col)));
                            }
                        }
                    inStmt.Append(")");
                    inStmt.Append("VALUES");
                    insertHeader = inStmt.ToString();
                    selectObjs.Append(" FROM ");
                    selectObjs.Append(QuotePath(tbl.FullName));
                    selectObjs.Append(" __o");
                    StarcounterEnvironment.RunWithinApplication(null, () => {
                        using (SqlEnumerator<IObjectView> selectEnum = (SqlEnumerator<IObjectView>)Db.SQL<IObjectView>(selectObjs.ToString()).GetEnumerator()) {
                            Debug.Assert(selectEnum.TypeBinding != null);
                            while (selectEnum.MoveNext()) {
                                IObjectView val = selectEnum.Current;
                                string valTypeName = null;
                                if (selectEnum.PropertyBinding == null) {
                                    Debug.Assert(selectEnum.TypeBinding.GetPropertyBinding(0).TypeCode == DbTypeCode.Object);
                                    Debug.Assert(selectEnum.TypeBinding.PropertyCount > 0);
                                    valTypeName = val.GetObject(0).GetType().ToString();
                                } else
                                    valTypeName = val.GetType().ToString();
                                Debug.Assert(valTypeName != null);
                                if (valTypeName == tbl.FullName) {
                                    if (tblNrObj == 0)
                                        inStmt.Append("(");
                                    else
                                        inStmt.Append(",(");
                                    if (selectEnum.PropertyBinding == null)
                                        inStmt.Append("object " + (val.GetObject(0).GetObjectNo() + shiftId).ToString()); // Value __id
                                    else
                                        inStmt.Append("object " + (val.GetObjectNo() + shiftId).ToString()); // Value __id
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
                            }
                        }
                    });

                    if (tblNrObj > 0)
                        using (StreamWriter file = new StreamWriter(fileName, true)) {
                            file.WriteLine(inStmt.ToString());
                        }
                    totalNrObj += tblNrObj;
                }
            }
            return totalNrObj;
        }

        internal static int Load(string filename) {
            int nrObjs = 0;
            using (StreamReader file = new StreamReader(filename)) {
                string stmt = file.ReadLine();
                if (stmt != "Database dump. DO NOT EDIT!")
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
                stmt = file.ReadLine();
                while (stmt != null) {
                    string nextStmt = file.ReadLine();
                    while (nextStmt != null && !nextStmt.StartsWith("INSERT")) {
                        stmt += nextStmt;
                        nextStmt = file.ReadLine();
                    }
                    Db.SystemTransact(delegate {
                        nrObjs += Db.Update(stmt);
                    });
                    stmt = nextStmt;
                }
            }
            return nrObjs;
        }

        internal static void DeleteAll() {
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true)) {
                Db.Transact(delegate {
                    Db.SlowSQL("DELETE FROM " + QuotePath(tbl.FullName));
                });
            }
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
                    return ((Decimal)decVal).ToString(CultureInfo.InvariantCulture);
                case DbTypeCode.Single:
                    {
                        float? fltVal = values.GetSingle(index);
                        if (fltVal == null)

                            return nullStr;
                        float val = fltVal.Value;
                        string str = val.ToString("R", CultureInfo.InvariantCulture);

                        if (float.IsInfinity(val) || float.IsNaN(val))
                            str = "'" + str + "'";
                        return str;
                    }
                case DbTypeCode.Double:
                    {
                        Double? doubVal = values.GetDouble(index);
                        if (doubVal == null)
                            return nullStr;

                        double val = doubVal.Value;
                        string str = val.ToString("G17", CultureInfo.InvariantCulture);

                        if (double.IsInfinity(val) || double.IsNaN(val))
                            str = "'" + str + "'";
                        return str;
                    }
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
