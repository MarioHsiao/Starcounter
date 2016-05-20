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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Starcounter
{
    public static class Reload
    {
        public static string QuoteName(string name)
        {
            return "\"" + name + "\"";
        }

        public static string QuotePath(string name)
        {
            int dotPos = -1;
            char dotChar = '.';
            StringBuilder quotedPath = new StringBuilder(name.Length + 6);
            while ((dotPos = name.IndexOf(dotChar)) > -1)
            {
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
        private static string GetPropertyName(Column col)
        {
            Debug.Assert(col.Table is RawView);

            var typeDef = Bindings.GetTypeDef(((RawView)col.Table).FullName);
            var prop = typeDef.PropertyDefs.FirstOrDefault((candidate) =>
            {
                return candidate.ColumnName == col.Name;
            });
            if (prop == null)
            {
                throw ErrorCode.ToException(
                    Error.SCERRCOLUMNHASNOPROPERTY,
                    string.Format("Missing property for {0}.{1}", col.Table.Name, col.Name));
            }

            return prop.Name;
        }

        private static IEnumerable<RawView> GetTablesForUnload(bool unloadAll)
        {
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true))
            {
                Debug.Assert(!String.IsNullOrEmpty(tbl.UniqueIdentifier));

                if (Binding.Bindings.GetTypeDef(tbl.FullName) == null)
                {
                    if (unloadAll)
                        throw ErrorCode.ToException(Error.SCERRUNLOADTABLENOCLASS,
                            "Table " + tbl.FullName + " cannot be unloaded.");
                    else
                        LogSources.Unload.LogWarning("Table " + tbl.FullName + " cannot be unloaded, since its class is not loaded.");
                    //Console.WriteLine("Warning: Table " + tbl.FullName + " cannot be unloaded, since its class is not loaded.");
                }
                else
                {
                    yield return tbl;
                }
            }
        }

        private static IEnumerable<Column> GetColumnsForUnload(Table tbl)
        {
            return Db.SQL<Column>("select c from starcounter.metadata.column c where c.table = ?", tbl)
                     .Where(col => (col.Name != "__id" && col.Name != "__setspecifier" && col.Name != "__type_id"));
        }

        private static string CreateSelectStatementForTable(Table tbl)
        {
            return String.Format("SELECT __o as __id {0} FROM {1} __o",
                                  String.Concat(GetColumnsForUnload(tbl).Select(col => "," + QuoteName(GetPropertyName(col)))),
                                  QuotePath(tbl.FullName));
        }

        private static string CreateInsertIntoHeaderForTable(Table tbl)
        {
            return String.Format("INSERT INTO {0}(__id{1})VALUES",
                                  QuotePath(tbl.UniqueIdentifier),
                                  String.Concat(GetColumnsForUnload(tbl).Select(col => "," + QuoteName(col.Name))));
        }

        private class ExportItem
        {
            public IObjectView val;
            public ITypeBinding TypeBinding;
            public IPropertyBinding PropertyBinding;

            public string GetTypeName()
            {
                if (PropertyBinding == null)
                {
                    Debug.Assert(TypeBinding.GetPropertyBinding(0).TypeCode == DbTypeCode.Object);
                    Debug.Assert(TypeBinding.PropertyCount > 0);
                    return val.GetObject(0).GetType().ToString();
                }
                else
                    return val.GetType().ToString();
            }

            public ulong GetObjectNo()
            {
                return (PropertyBinding == null) ?
                            val.GetObject(0).GetObjectNo() :
                            val.GetObjectNo();
            }
        }

        private static IEnumerable<ExportItem> GetExportItems(string selectObjs)
        {
            using (SqlEnumerator<IObjectView> selectEnum = (SqlEnumerator<IObjectView>)Db.SQL<IObjectView>(selectObjs).GetEnumerator())
            {
                Debug.Assert(selectEnum.TypeBinding != null);

                while (selectEnum.MoveNext())
                {
                    yield return new ExportItem()
                    {
                        val = selectEnum.Current,
                        PropertyBinding = selectEnum.PropertyBinding,
                        TypeBinding = selectEnum.TypeBinding
                    };
                }
            }
        }



        private static string ToValuesClause(this ExportItem e, ulong shiftId)
        {
            Debug.Assert(e.TypeBinding != null);

            return String.Format("(object {0}{1})",
                                 e.GetObjectNo() + shiftId,
                                 String.Concat(Enumerable.Range(1, Math.Max(e.TypeBinding.PropertyCount - 1, 0))
                                                          .Select(i => "," + GetString(e.val, i, shiftId))));
        }

        private static int UnloadItemsInParallel(BlockingCollection<IEnumerable<ExportItem>> items, string insertHeader, ulong shiftId, string fileName)
        {
            int tblNrObj = 0;
            int curr_row_count = 0;

            var inStmt = new StringBuilder();
            inStmt.Append(insertHeader);

            foreach (string insert_stmt in items.GetConsumingEnumerable()
                                                .Select(ie => ie.Select<ExportItem, Func<string>>( e=> ()=>e.ToValuesClause(shiftId)))
                                                .DoParallelTransact()
                                                .SelectMany(s=>s))
            {
                if (curr_row_count != 0)
                    inStmt.Append(",");

                inStmt.Append(insert_stmt);

                curr_row_count++;
                if (curr_row_count == 1000)
                {
                    tblNrObj += curr_row_count;
                    curr_row_count = 0;

                    using (StreamWriter file = new StreamWriter(fileName, true))
                    {
                        file.WriteLine(inStmt.ToString());
                    }
                    inStmt = new StringBuilder();
                    inStmt.Append(insertHeader);
                }
            }
            if (curr_row_count != 0)
            {
                tblNrObj += curr_row_count;
                using (StreamWriter file = new StreamWriter(fileName, true))
                {
                    file.WriteLine(inStmt.ToString());
                }
            }

            return tblNrObj;

        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> source, int partition_size)
        {
            var partition = new List<T>(partition_size);

            foreach (var x in source)
            {
                partition.Add(x);
                if (partition.Count == partition_size)
                {
                    yield return partition;
                    partition = new List<T>(partition_size);
                }
            }
            if (partition.Any())
            {
                yield return partition;
            }
        }

        internal static int Unload(string fileName, ulong shiftId, Boolean unloadAll)
        {
            int totalNrObj = 0;
            // Create empty file
            using (StreamWriter fileStream = new StreamWriter(fileName, false))
            {
                fileStream.WriteLine("Database dump. DO NOT EDIT!");
            }
            foreach (RawView tbl in GetTablesForUnload(unloadAll))
            {
                string selectObjs = CreateSelectStatementForTable(tbl);
                string insertHeader = CreateInsertIntoHeaderForTable(tbl);

                BlockingCollection<IEnumerable<ExportItem>> items = new BlockingCollection<IEnumerable<ExportItem>>(Environment.ProcessorCount);
                System.Threading.Tasks.Task<int> export = System.Threading.Tasks.Task.Run(() => UnloadItemsInParallel(items, insertHeader, shiftId, fileName));
                try {
                    StarcounterEnvironment.RunWithinApplication(null, () => {
                        foreach (IEnumerable<ExportItem> e in GetExportItems(selectObjs).Where(e => e.GetTypeName() == tbl.FullName).Partition(100)) {
                            items.Add(e);
                        }
                    });
                } finally {
                    items.CompleteAdding();
                }

                totalNrObj += export.Result;
            }
            return totalNrObj;
        }

        private static IEnumerable<IEnumerable<T>> SplitBy<T>(this IEnumerable<T> seq, Func<T, bool> pred)
        {
            List<T> current_subseq = new List<T>();

            foreach (var t in seq)
            {
                if (pred(t) && current_subseq.Count > 0)
                {
                    yield return current_subseq;
                    current_subseq.Clear();
                }
                current_subseq.Add(t);
            }

            yield return current_subseq;
        }

        private static IEnumerable<IEnumerable<T>> DoParallelTransact<T>(this IEnumerable<IEnumerable<Func<T>>> actions)
        {
            byte schedulers_count = Starcounter.Internal.StarcounterEnvironment.SchedulerCount;
            BlockingCollection<byte> free_schedulers = new BlockingCollection<byte>(schedulers_count);

            for (byte scheduler = 0; scheduler < schedulers_count; ++scheduler)
                free_schedulers.Add(scheduler);


            return actions.AsParallel().Select(a =>
            {
                string app_name = Starcounter.Internal.StarcounterEnvironment.AppName;

                byte scheduler = free_schedulers.Take();
                var res = new List<T>(100);

                try
                {
                    Scheduling.ScheduleTask(() =>
                        Starcounter.Internal.StarcounterEnvironment.RunWithinApplication(app_name, () =>
                        {
                            Db.Transact(() =>
                            {
                                foreach (var i in a)
                                    res.Add(i());
                            });
                        }), true, scheduler);

                    return res;
                }
                finally
                {
                    free_schedulers.Add(scheduler);
                }
            });
        }

        internal static int Load(string filename)
        {
            var lines = File.ReadLines(filename);

            if (lines.FirstOrDefault() != "Database dump. DO NOT EDIT!")
                throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);

            return lines.Skip(1)
                        .SplitBy(line => line.StartsWith("INSERT"))
                        .Select(lines_collection => String.Concat(lines_collection))
                        .Select(statement => new Func<int>[] { () => Db.Update(statement) })
                        .DoParallelTransact()
                        .SelectMany(n=>n)
                        .Sum();
        }

        internal static void DeleteAll()
        {
            foreach (RawView tbl in Db.SQL<RawView>("select t from rawview t where updatable = ?", true))
            {
                Db.Transact(delegate
                {
                    Db.SlowSQL("DELETE FROM " + QuotePath(tbl.FullName));
                });
            }
        }

        public static string GetString(IObjectView values, int index, ulong shiftId)
        {
            string nullStr = "NULL";
            DbTypeCode typeCode = values.TypeBinding.GetPropertyBinding(index).TypeCode;
            switch (typeCode)
            {
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
