﻿// ***********************************************************************
// <copyright file="TableUpgrade.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Binding
{

    /// <summary>
    /// Class TableUpgrade
    /// </summary>
    public class TableUpgrade
    {

        /// <summary>
        /// The pending update table name prefix
        /// </summary>
        public const string PendingUpdateTableNamePrefix = "0";

        /// <summary>
        /// Creates the name of the pending update table.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        public static string CreatePendingUpdateTableName(string name)
        {
            // TODO: Restrict name size and format on non upgrade table.
            return string.Concat(PendingUpdateTableNamePrefix, name); 
        }

        /// <summary>
        /// Delegate RecordHandler
        /// </summary>
        /// <param name="obj">The obj.</param>
        private delegate void RecordHandler(ObjectRef obj);

        /// <summary>
        /// The table name_
        /// </summary>
        private readonly string tableName_;
        /// <summary>
        /// The old table def_
        /// </summary>
        private readonly TableDef oldTableDef_;
        /// <summary>
        /// The new table def_
        /// </summary>
        private TableDef newTableDef_;

        /// <summary>
        /// The column value transfer set_
        /// </summary>
        private ColumnValueTransfer[] columnValueTransferSet_;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableUpgrade" /> class.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="oldTableDef">The old table def.</param>
        /// <param name="newTableDef">The new table def.</param>
        public TableUpgrade(string tableName, TableDef oldTableDef, TableDef newTableDef)
        {
            tableName_ = tableName;
            oldTableDef_ = oldTableDef;
            newTableDef_ = newTableDef;
        }

        /// <summary>
        /// Evals this instance.
        /// </summary>
        /// <returns>TableDef.</returns>
        public TableDef Eval()
        {
            CreateNewTable();

            // We do the inheriting tables first so that only the records of
            // current table remains when we scan the,

            TableDef[] directlyInheritedTableDefs = GetDirectlyInheritedTableDefs(oldTableDef_.TableId);
            for (int i = 0; i < directlyInheritedTableDefs.Length; i++)
            {
                UpgradeInheritingTable(directlyInheritedTableDefs[i]);
            }

            BuildColumnValueTransferSet();

            MoveRecordsToNewTable();

            // TODO: Add all indexes defined on old table to new table.

            DropOldTable();

            RenameNewTable();

            Db.Transaction(() =>
            {
                newTableDef_ = Db.LookupTable(oldTableDef_.Name);
            });

            return newTableDef_;
        }

        /// <summary>
        /// Continues the eval.
        /// </summary>
        /// <returns>TableDef.</returns>
        public TableDef ContinueEval()
        {
            if (oldTableDef_ != null)
            {
                // Start or conclude any upgrades on inherited tables.
                
                // If an inherited table already has been upgraded it won't
                // show in this list since it will no longer inherit the old
                // table but will instead inherit the new table.

                TableDef[] directlyInheritedTableDefs;
                directlyInheritedTableDefs = GetDirectlyInheritedTableDefs(oldTableDef_.TableId);
                for (int i = 0; i < directlyInheritedTableDefs.Length; i++)
                {
                    ContinueUpgradeInheritingTable(directlyInheritedTableDefs[i]);
                }

                // If an upgrade of an inherited table is concluded but the
                // table rename was not completed we will have a table
                // inheriting the new table but with an illegal name. So we
                // check all the tables inheriting the new table and make sure
                // they have the final name.
                //
                // This we do not have to do recursivly since if the replaced
                // inherited table is dropped then all upgrades on table
                // inherting this table will have been completed.

                directlyInheritedTableDefs = GetDirectlyInheritedTableDefs(newTableDef_.TableId);
                for (int i = 0; i < directlyInheritedTableDefs.Length; i++)
                {
                    var directlyInheritedTableDef = directlyInheritedTableDefs[i];
                    var inheritedTableName = directlyInheritedTableDef.Name;
                    if (inheritedTableName.StartsWith(PendingUpdateTableNamePrefix))
                    {
                        inheritedTableName = inheritedTableName.Substring(PendingUpdateTableNamePrefix.Length);
                        Db.RenameTable(directlyInheritedTableDef.TableId, inheritedTableName);
                    }
                }

                BuildColumnValueTransferSet();

                MoveRecordsToNewTable();

                // TODO:
                // Assure that all indexes defined on old table is defined on new table.

                DropOldTable();
            }

            RenameNewTable();

            Db.Transaction(() =>
            {
                newTableDef_ = Db.LookupTable(oldTableDef_.Name);
            });

            return newTableDef_;
        }

        /// <summary>
        /// Creates the new table.
        /// </summary>
        private void CreateNewTable()
        {
            newTableDef_ = newTableDef_.Clone();
            newTableDef_.Name = CreatePendingUpdateTableName(newTableDef_.Name);
            var tableCreate = new TableCreate(newTableDef_);
            newTableDef_ = tableCreate.Eval();
        }

        /// <summary>
        /// Upgrades the inheriting table.
        /// </summary>
        /// <param name="oldInheritingTableDef">The old inheriting table def.</param>
        private void UpgradeInheritingTable(TableDef oldInheritingTableDef)
        {
            List<ColumnDef> newColumnDefs = new List<ColumnDef>();
            ColumnDef[] inheritedColumnDefs = newTableDef_.ColumnDefs;
            for (int i = 0; i < inheritedColumnDefs.Length; i++)
            {
                var inheritedColumnDef = inheritedColumnDefs[i].Clone();
                inheritedColumnDef.IsInherited = true;
                newColumnDefs.Add(inheritedColumnDef);
            }

            ColumnDef[] columnDefs = oldInheritingTableDef.ColumnDefs;
            for (int i = oldTableDef_.ColumnDefs.Length; i < columnDefs.Length; i++)
            {
                newColumnDefs.Add(columnDefs[i].Clone());
            }

            string tableName = oldInheritingTableDef.Name;

            TableDef newInheritingTableDef = new TableDef(
                tableName,
                newTableDef_.Name,
                newColumnDefs.ToArray()
                );

            var tableUpgrade = new TableUpgrade(tableName, oldInheritingTableDef, newInheritingTableDef);
            tableUpgrade.Eval();
        }

        /// <summary>
        /// Continues the upgrade inheriting table.
        /// </summary>
        /// <param name="oldInheritingTableDef">The old inheriting table def.</param>
        private void ContinueUpgradeInheritingTable(TableDef oldInheritingTableDef)
        {
            var tableName = oldInheritingTableDef.Name;
            TableDef newInheritingTableDef = null;

            Db.Transaction(() =>
            {
                newInheritingTableDef = Db.LookupTable(CreatePendingUpdateTableName(tableName));
            });

            if (newInheritingTableDef != null)
            {
                var tableUpgrade = new TableUpgrade(tableName, oldInheritingTableDef, newInheritingTableDef);
                tableUpgrade.ContinueEval();
            }
            else
            {
                UpgradeInheritingTable(oldInheritingTableDef);
            }
        }

        /// <summary>
        /// Builds the column value transfer set.
        /// </summary>
        /// <exception cref="System.NotSupportedException"></exception>
        private void BuildColumnValueTransferSet()
        {
            List<ColumnValueTransfer> output = new List<ColumnValueTransfer>();

            ColumnDef[] oldColumnDefs = oldTableDef_.ColumnDefs;
            ColumnDef[] newColumnDefs = newTableDef_.ColumnDefs;

            for (int oi = 0; oi < oldColumnDefs.Length; oi++)
            {
                var oldColumnDef = oldColumnDefs[oi];
                for (int ni = 0; ni < newColumnDefs.Length; ni++)
                {
                    var newColumnDef = newColumnDefs[ni];
                    if (oldColumnDef.Equals(newColumnDef))
                    {
                        switch (newColumnDef.Type)
                        {
                        case DbTypeCode.Boolean:
                            output.Add(new BooleanColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Byte:
                        case DbTypeCode.DateTime:
                        case DbTypeCode.UInt64:
                        case DbTypeCode.UInt32:
                        case DbTypeCode.UInt16:
                            output.Add(new UInt64ColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Decimal:
                            output.Add(new DecimalColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Single:
                            output.Add(new SingleColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Double:
                            output.Add(new DoubleColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Int64:
                        case DbTypeCode.Int32:
                        case DbTypeCode.Int16:
                        case DbTypeCode.SByte:
                            output.Add(new Int64ColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Object:
                            output.Add(new ObjectColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.String:
                            output.Add(new StringColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.Binary:
                            output.Add(new BinaryColumnValueTransfer(oi, ni));
                            break;
                        case DbTypeCode.LargeBinary:
                            output.Add(new LargeBinaryColumnValueTransfer(oi, ni));
                            break;
                        default:
                            throw new NotSupportedException();
                        }
                    }
                }
            }

            columnValueTransferSet_ = output.ToArray();
        }

        /// <summary>
        /// Moves the records to new table.
        /// </summary>
        private void MoveRecordsToNewTable()
        {
            var indexInfo = oldTableDef_.GetAllIndexInfos()[0];
            Byte[] lk, hk;
            BuildScanRangeKeys(indexInfo, out lk, out hk);
            var recordHandler = new RecordHandler(MoveRecord);
            ulong c = 0;
            do
            {
                Db.Transaction(() =>
                {
                    c = ScanDo(indexInfo.Handle, lk, hk, 1000, recordHandler);
                });
            }
            while (c != 0);
        }

        /// <summary>
        /// </summary>
        private unsafe TableDef[] GetDirectlyInheritedTableDefs(ushort baseTableId)
        {
            TableDef[] output = null;

            Db.Transaction(() =>
            {
                sccoredb.SCCOREDB_TABLE_INFO tableInfo;
                sccoredb.sccoredb_get_table_info_by_id(baseTableId, out tableInfo);

                var tableIds = new ushort[tableInfo.inheriting_table_count];
                for (var i = 0; i < tableIds.Length; i++)
                {
                    tableIds[i] = tableInfo.inheriting_table_ids[i];
                }

                var tableDefs = new List<TableDef>((int)tableInfo.inheriting_table_count);
                for (var i = 0; i < tableIds.Length; i++)
                {
                    var tableId = tableIds[i];
                    sccoredb.sccoredb_get_table_info_by_id(tableId, out tableInfo);
                    if (tableInfo.inherited_table_id == baseTableId)
                    {
                        tableDefs.Add(TableDef.ConstructTableDef(tableInfo));
                    }
                }

                output = tableDefs.ToArray();
            });

            return output;
        }

        /// <summary>
        /// Drops the old table.
        /// </summary>
        private void DropOldTable()
        {
            Db.DropTable(oldTableDef_.Name);
        }

        /// <summary>
        /// Renames the new table.
        /// </summary>
        private void RenameNewTable()
        {
            Db.RenameTable(newTableDef_.TableId, tableName_);
        }

        /// <summary>
        /// Moves the record.
        /// </summary>
        /// <param name="source">The source.</param>
        private void MoveRecord(ObjectRef source)
        {
            ColumnValueTransfer[] columnValueTransfers = columnValueTransferSet_;
            for (int i = 0; i < columnValueTransfers.Length; i++)
            {
                var columnValueTransfer = columnValueTransfers[i];
                columnValueTransfer.Read(source);
            }

            uint e;
            unsafe
            {
                e = sccoredb.sccoredb_replace(source.ObjectID, source.ETI, newTableDef_.TableId);
            }
            if (e == 0)
            {
                ObjectRef target = source;
                for (int i = 0; i < columnValueTransfers.Length; i++)
                {
                    var columnValueTransfer = columnValueTransfers[i];
                    columnValueTransfer.Write(target);
                }
            }
            else
            {
                throw ErrorCode.ToException(e);
            }
        }

        /// <summary>
        /// Builds the scan range keys.
        /// </summary>
        /// <param name="indexInfo">The index info.</param>
        /// <param name="lk">The lk.</param>
        /// <param name="hk">The hk.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        /// <exception cref="System.NotSupportedException"></exception>
        private unsafe void BuildScanRangeKeys(IndexInfo indexInfo, out Byte[] lk, out Byte[] hk)
        {
            lk = new Byte[64];
            hk = new Byte[64];
            
            fixed (byte* ulk = lk, uhk = hk)
            {
                byte* l = ulk; l += 4;
                byte* h = uhk; h += 4;

                for (int i = 0; i < indexInfo.AttributeCount; i++)
                {
                    // TODO: Handle sort order.
                    *l = 0; l++;
                    *h = 1; h++;
                    switch (indexInfo.GetTypeCode(i))
                    {
                        case DbTypeCode.Boolean:
                        case DbTypeCode.Byte:
                        case DbTypeCode.DateTime:
                        case DbTypeCode.UInt64:
                        case DbTypeCode.UInt32:
                        case DbTypeCode.UInt16:
                        case DbTypeCode.Object:
                            *((ulong*)h) = ulong.MaxValue; h += 8;
                            break;
                        case DbTypeCode.Decimal:
                            throw new NotImplementedException();
                        case DbTypeCode.Int64:
                        case DbTypeCode.Int32:
                        case DbTypeCode.Int16:
                        case DbTypeCode.SByte:
                            *((long*)h) = long.MaxValue; h += 8;
                            break;
                        case DbTypeCode.String:
                            byte* s;
                            sccoredb.SCConvertUTF16StringToNative("", 1, &s);
                            uint sl = *((uint*)s) + 4;
                            for (uint si = 0; si < sl; si++) *h++ = *s++;
                            break;
                        case DbTypeCode.Binary:
                            throw new NotImplementedException();
                        default:
                            throw new NotSupportedException();
                    }
                }

                *((uint*)ulk) = (uint)(l - ulk);
                *((uint*)uhk) = (uint)(h - uhk);
            }
        }

        /// <summary>
        /// Scans the do.
        /// </summary>
        /// <param name="indexHandle">The index handle.</param>
        /// <param name="lk">The lk.</param>
        /// <param name="hk">The hk.</param>
        /// <param name="max">The max.</param>
        /// <param name="handler">The handler.</param>
        /// <returns>System.UInt64.</returns>
        private unsafe ulong ScanDo(ulong indexHandle, byte[] lk, byte[] hk, ulong max, RecordHandler handler)
        {
            uint e;

            ushort filterTableId = oldTableDef_.TableId;

            ulong count = 0;

            ulong hiter;
            ulong viter;
            fixed (byte* ulk = lk, uhk = hk)
            {
                e = sccoredb.SCIteratorCreate(
                    indexHandle,
                    0,
                    ulk,
                    uhk,
                    &hiter,
                    &viter
                    );
            }
            if (e == 0)
            {
                try
                {
                    for (; ; )
                    {
                        ObjectRef source;
                        ushort tableId;
                        ulong dummy;

                        e = sccoredb.SCIteratorNext(hiter, viter, &source.ObjectID, &source.ETI, &tableId, &dummy);
                        if (e == 0)
                        {
                            if (source.ObjectID != sccoredb.MDBIT_OBJECTID)
                            {
                                if (tableId == filterTableId)
                                {
                                    handler(source);
                                    if (++count == max) break;
                                }
                            }
                            else break;
                        }
                        else break;
                    }
                }
                finally
                {
                    e = sccoredb.SCIteratorFree(hiter, viter);
                }
            }
            if (e != 0) throw ErrorCode.ToException(e);
            return count;
        }
    }


    /// <summary>
    /// Class ColumnValueTransfer
    /// </summary>
    internal abstract class ColumnValueTransfer
    {

        /// <summary>
        /// Class UpgradeRecord
        /// </summary>
        internal sealed class UpgradeRecord : Entity
        {

            /// <summary>
            /// Initializes a new instance of the <see cref="UpgradeRecord" /> class.
            /// </summary>
            /// <param name="u">The u.</param>
            public UpgradeRecord(Uninitialized u) : base(u) { }
        }

        /// <summary>
        /// The rec_
        /// </summary>
        protected UpgradeRecord rec_;
        /// <summary>
        /// The source index_
        /// </summary>
        protected readonly int sourceIndex_;
        /// <summary>
        /// The target index_
        /// </summary>
        protected readonly int targetIndex_;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public ColumnValueTransfer(int sourceIndex, int targetIndex)
        {
            rec_ = new UpgradeRecord((Uninitialized)null);
            sourceIndex_ = sourceIndex;
            targetIndex_ = targetIndex;
        }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public abstract void Read(ObjectRef source);

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public abstract void Write(ObjectRef target);
    }

    /// <summary>
    /// Class BooleanColumnValueTransfer
    /// </summary>
    internal class BooleanColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private bool? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public BooleanColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableBoolean(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteBoolean(rec_, targetIndex_, value_.Value);
        }
    }

    /// <summary>
    /// Class BinaryColumnValueTransfer
    /// </summary>
    internal class BinaryColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private Binary value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public BinaryColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadBinary(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteBinary(rec_, targetIndex_, value_);
        }
    }

    /// <summary>
    /// Class DecimalColumnValueTransfer
    /// </summary>
    internal class DecimalColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private decimal? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public DecimalColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableDecimal(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteDecimal(rec_, targetIndex_, value_.Value);
        }
    }

    /// <summary>
    /// Class DoubleColumnValueTransfer
    /// </summary>
    internal class DoubleColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private double? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="DoubleColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public DoubleColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableDouble(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteDouble(rec_, targetIndex_, value_.Value);
        }
    }

    /// <summary>
    /// Class Int64ColumnValueTransfer
    /// </summary>
    internal class Int64ColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private long? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="Int64ColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public Int64ColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableInt64(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteInt64(rec_, targetIndex_, value_.Value);
        }
    }

    /// <summary>
    /// Class LargeBinaryColumnValueTransfer
    /// </summary>
    internal class LargeBinaryColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private LargeBinary value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="LargeBinaryColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public LargeBinaryColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadLargeBinary(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteLargeBinary(rec_, targetIndex_, value_);
        }
    }

    /// <summary>
    /// Class ObjectColumnValueTransfer
    /// </summary>
    internal class ObjectColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private ObjectRef value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public ObjectColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            value_ = DoRead(source);
        }

        /// <summary>
        /// Does the read.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>ObjectRef.</returns>
        private ObjectRef DoRead(ObjectRef source)
        {
            UInt16 flags;
            ObjectRef value;
            UInt16 cci;
            UInt32 ec;
            flags = 0;
            unsafe
            {
                sccoredb.Mdb_ObjectReadObjRef(
                    source.ObjectID,
                    source.ETI,
                    sourceIndex_,
                    &value.ObjectID,
                    &value.ETI,
                    &cci,
                    &flags
                );
            }
            if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0)
            {
                if ((flags & sccoredb.Mdb_DataValueFlag_Null) == 0)
                {
                    return value;
                }
                else
                {
                    value.ObjectID = sccoredb.MDBIT_OBJECTID;
                    value.ETI = sccoredb.INVALID_RECORD_ADDR;
                    return value;
                }
            }
            ec = sccoredb.Mdb_GetLastError();
            throw ErrorCode.ToException(ec);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            if (value_.ObjectID != sccoredb.MDBIT_OBJECTID)
            {
                Boolean br;
                br = sccoredb.Mdb_ObjectWriteObjRef(
                         target.ObjectID,
                         target.ETI,
                         targetIndex_,
                         value_.ObjectID,
                         value_.ETI
                     );
                if (br)
                {
                    return;
                }
                throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
            }
        }
    }

    /// <summary>
    /// Class SingleColumnValueTransfer
    /// </summary>
    internal class SingleColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private float? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public SingleColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableSingle(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteSingle(rec_, targetIndex_, value_.Value);
        }
    }

    /// <summary>
    /// Class StringColumnValueTransfer
    /// </summary>
    internal class StringColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private string value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public StringColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadString(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteString(rec_, targetIndex_, value_);
        }
    }

    /// <summary>
    /// Class UInt64ColumnValueTransfer
    /// </summary>
    internal class UInt64ColumnValueTransfer : ColumnValueTransfer
    {

        /// <summary>
        /// The value_
        /// </summary>
        private ulong? value_;

        /// <summary>
        /// Initializes a new instance of the <see cref="UInt64ColumnValueTransfer" /> class.
        /// </summary>
        /// <param name="sourceIndex">Index of the source.</param>
        /// <param name="targetIndex">Index of the target.</param>
        public UInt64ColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        /// <summary>
        /// Reads the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableUInt64(rec_, sourceIndex_);
        }

        /// <summary>
        /// Writes the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteUInt64(rec_, targetIndex_, value_.Value);
        }
    }
}
