
using Sc.Server.Binding;
using Sc.Server.Internal;
using Starcounter.Internal;
using System;
using System.Collections.Generic;

namespace Starcounter.Binding
{

    internal class TableUpgrade
    {

        private delegate void RecordHandler(ObjectRef obj);

        private readonly TableDef oldTableDef_;
        private TableDef newTableDef_;

        private ColumnValueTransfer[] columnValueTransferSet_;

        public TableUpgrade(TableDef oldTableDef, TableDef newTableDef)
        {
            oldTableDef_ = oldTableDef;
            newTableDef_ = newTableDef;
        }

        public TableDef Eval()
        {
            CreateNewTable();

            BuildColumnValueTransferSet();

            // We do the inheriting tables first so that only the records of
            // current table remains when we scan the,

            TableDef[] directlyInheritedTableDefs = GetDirectlyInheritedTableDefs();
            for (int i = 0; i < directlyInheritedTableDefs.Length; i++)
            {
                UpgradeInheritingTable(directlyInheritedTableDefs[i]);
            }

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

        private void CreateNewTable()
        {
            // TODO: Restrict name size and format on non upgrade table.
            newTableDef_ = newTableDef_.Clone();
            newTableDef_.Name = string.Concat("0", newTableDef_.Name, "000");
            var tableCreate = new TableCreate(newTableDef_);
            newTableDef_ = tableCreate.Eval();
        }

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

            TableDef newInheritingTableDef = new TableDef(
                oldInheritingTableDef.Name,
                newTableDef_.Name,
                newColumnDefs.ToArray()
                );

            TableUpgrade tableUpgrade = new TableUpgrade(oldInheritingTableDef, newInheritingTableDef);
            tableUpgrade.Eval();
        }

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

        private unsafe TableDef[] GetDirectlyInheritedTableDefs()
        {
            TableDef[] output = null;

            Db.Transaction(() =>
            {
                sccoredb.Mdb_DefinitionInfo definitionInfo;
                sccoredb.Mdb_DefinitionToDefinitionInfo(oldTableDef_.DefinitionAddr, out definitionInfo);

                ulong[] definitionAddrs = new ulong[definitionInfo.inheriting_definition_count];
                for (int i = 0; i < definitionAddrs.Length; i++)
                {
                    definitionAddrs[i] = definitionInfo.inheriting_definition_addrs[i];
                }

                List<TableDef> tableDefs = new List<TableDef>((int)definitionInfo.inheriting_definition_count);

                for (int i = 0; i < definitionAddrs.Length; i++)
                {
                    ulong definitionAddr = definitionAddrs[i];
                    sccoredb.Mdb_DefinitionToDefinitionInfo(definitionAddr, out definitionInfo);
                    if (definitionInfo.inherited_definition_addr == oldTableDef_.DefinitionAddr)
                    {
                        tableDefs.Add(TableDef.ConstructTableDef(definitionAddr, definitionInfo));
                    }
                }

                output = tableDefs.ToArray();
            });

            return output;
        }

        private void DropOldTable()
        {
            Db.DropTable(oldTableDef_.TableId);
        }

        private void RenameNewTable()
        {
            Db.RenameTable(newTableDef_.TableId, oldTableDef_.Name);
        }

        private void MoveRecord(ObjectRef source)
        {
            uint e;
            ObjectRef target;

            unsafe
            {
                e = sccoredb.sc_insert(newTableDef_.DefinitionAddr, &target.ObjectID, &target.ETI);
            }
            if (e == 0)
            {
                ColumnValueTransfer[] columnValueTransfers = columnValueTransferSet_;
                for (int i = 0; i < columnValueTransfers.Length; i++)
                {
                    ColumnValueTransfer columnValueTransfer = columnValueTransfers[i];
                    columnValueTransfer.Read(source);
                    columnValueTransfer.Write(target);
                }

                int b;
                b = sccoredb.Mdb_ObjectIssueDelete(source.ObjectID, source.ETI);
                if (b != 0) b = sccoredb.Mdb_ObjectDelete(source.ObjectID, source.ETI, 1);
                if (b == 0) e = sccoredb.Mdb_GetLastError();
            }

            if (e != 0) throw ErrorCode.ToException(e);
        }

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


    internal abstract class ColumnValueTransfer
    {

        internal sealed class UpgradeRecord : DbObject { }

        protected UpgradeRecord rec_;
        protected readonly int sourceIndex_;
        protected readonly int targetIndex_;

        public ColumnValueTransfer(int sourceIndex, int targetIndex)
        {
            rec_ = new UpgradeRecord();
            sourceIndex_ = sourceIndex;
            targetIndex_ = targetIndex;
        }

        public abstract void Read(ObjectRef source);

        public abstract void Write(ObjectRef target);
    }

    internal class BooleanColumnValueTransfer : ColumnValueTransfer
    {

        private bool? value_;

        public BooleanColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableBoolean(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteBoolean(rec_, targetIndex_, value_.Value);
        }
    }

    internal class BinaryColumnValueTransfer : ColumnValueTransfer
    {

        private Binary value_;

        public BinaryColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadBinary(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteBinary(rec_, targetIndex_, value_);
        }
    }

    internal class DecimalColumnValueTransfer : ColumnValueTransfer
    {

        private decimal? value_;

        public DecimalColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableDecimal(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteDecimal(rec_, targetIndex_, value_.Value);
        }
    }

    internal class DoubleColumnValueTransfer : ColumnValueTransfer
    {

        private double? value_;

        public DoubleColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableDouble(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteDouble(rec_, targetIndex_, value_.Value);
        }
    }

    internal class Int64ColumnValueTransfer : ColumnValueTransfer
    {

        private long? value_;

        public Int64ColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableInt64(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteInt64(rec_, targetIndex_, value_.Value);
        }
    }

    internal class LargeBinaryColumnValueTransfer : ColumnValueTransfer
    {

        private LargeBinary value_;

        public LargeBinaryColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadLargeBinary(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteLargeBinary(rec_, targetIndex_, value_);
        }
    }

    internal class ObjectColumnValueTransfer : ColumnValueTransfer
    {

        private ObjectRef value_;

        public ObjectColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            value_ = DoRead(source);
        }

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

    internal class SingleColumnValueTransfer : ColumnValueTransfer
    {

        private float? value_;

        public SingleColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableSingle(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteSingle(rec_, targetIndex_, value_.Value);
        }
    }

    internal class StringColumnValueTransfer : ColumnValueTransfer
    {

        private string value_;

        public StringColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadString(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            DbState.WriteString(rec_, targetIndex_, value_);
        }
    }

    internal class UInt64ColumnValueTransfer : ColumnValueTransfer
    {

        private ulong? value_;

        public UInt64ColumnValueTransfer(int sourceIndex, int targetIndex) : base(sourceIndex, targetIndex) { }

        public override void Read(ObjectRef source)
        {
            rec_.ThisRef = source;
            value_ = DbState.ReadNullableUInt64(rec_, sourceIndex_);
        }

        public override void Write(ObjectRef target)
        {
            rec_.ThisRef = target;
            if (value_.HasValue)
                DbState.WriteUInt64(rec_, targetIndex_, value_.Value);
        }
    }
}
