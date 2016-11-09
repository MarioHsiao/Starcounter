using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Binding;
using Starcounter.Internal.Metadata;

namespace QueryProcessingTest {
    public static class MetadataTest {
        public static void TestPopulatedMetadata() {
            HelpMethods.LogEvent("Test populated meta-data");
            TestTypeMetadata();
            TestRuntimeColumnMetadata();
            ClrMetadatTest();
            TestRuntimeIndexBasedOnMat();
            TestRuntimeIndex();
            HelpMethods.LogEvent("Finished testing populated meta-data");
        }

        public static void TestTypeMetadata() {
            Starcounter.Metadata.DbPrimitiveType t =
                Db.SQL<Starcounter.Metadata.DbPrimitiveType>("select t from dbPrimitiveType t order by primitivetype").First;
            Trace.Assert(t != null);
            Trace.Assert(t.Name == "string");
            Trace.Assert(t.PrimitiveType == sccoredb.STAR_TYPE_STRING);
            ulong acc = 0;
            int count = 0;
            foreach (Starcounter.Metadata.DbPrimitiveType mt in
                Db.SQL<Starcounter.Metadata.DbPrimitiveType>("select t from DbPrimitiveType t")) {
                    Trace.Assert(!String.IsNullOrWhiteSpace(mt.Name));
                acc += mt.PrimitiveType;
                count++;
            }
            Trace.Assert(count == 9);
            Trace.Assert(acc == 45);
            Starcounter.Metadata.MapPrimitiveType mapt = 
                Db.SQL<Starcounter.Metadata.MapPrimitiveType>("select t from MapPrimitiveType t where name = ?", 
                "Int16").First;
            Trace.Assert(mapt != null);
            Trace.Assert(mapt is ClrPrimitiveType);
            Trace.Assert(mapt.DbPrimitiveType.Name == "long");
            Trace.Assert(!mapt.WriteLoss);
            Trace.Assert(mapt.ReadLoss);
            acc = 0;
            count = 0;
            foreach (Starcounter.Metadata.MapPrimitiveType mpt in 
                Db.SQL<Starcounter.Metadata.MapPrimitiveType>("select t from MapPrimitiveType t")) {
                Trace.Assert(mpt.DbPrimitiveType != null);
                Trace.Assert(!String.IsNullOrWhiteSpace(mpt.Name));
                count++;
            }
            Trace.Assert(count == 15);
            RawView rv = Db.SQL<RawView>("select rw from rawview rw where fullname = ?", 
                "Starcounter.Metadata.DataType").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.UniqueIdentifierReversed == "DataType.Metadata.Starcounter.Raw.Starcounter");
            Trace.Assert(!rv.Updatable);
            //Trace.Assert(rv.Inherits == null);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "ClrClass").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.UniqueIdentifierReversed == "ClrClass.Metadata.Starcounter.Raw.Starcounter");
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits != null);
            Trace.Assert(rv.Inherits.FullName == "Starcounter.Metadata.VMView");
            Trace.Assert(rv.Inherits.Name == "VMView");
            Trace.Assert(rv.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.FullName == "Starcounter.Metadata.Table");
            Trace.Assert(rv.Inherits.Inherits.Name == "Table");
            Trace.Assert(rv.Inherits.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits.FullName == "Starcounter.Metadata.DataType");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Name == "DataType");
            //Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits == null);
            count = 0;
            foreach (Starcounter.Metadata.MapPrimitiveType mt in 
                Db.SQL<Starcounter.Metadata.MapPrimitiveType>("select t from MapPrimitiveType t")) {
                Trace.Assert(mt is ClrPrimitiveType);
                Starcounter.Binding.DbTypeCode typeCode = (Starcounter.Binding.DbTypeCode)(mt as ClrPrimitiveType).DbTypeCode;
                Trace.Assert(mt.Name == Enum.GetName(typeof(Starcounter.Binding.DbTypeCode), typeCode));
                count++;
            }
            Trace.Assert(count == 15);
            Trace.Assert(Db.SQL<Starcounter.Metadata.Table>("select t from \"table\" t").First != null);
            foreach (Starcounter.Metadata.Table table in
                Db.SQL<Starcounter.Metadata.Table>("select t from Starcounter.Metadata.Table t")) {
                    Trace.Assert(!String.IsNullOrWhiteSpace(table.Name));
            }
            foreach (String name in
                Db.SQL<String>("select Name from Starcounter.Metadata.Table")) {
                Trace.Assert(!String.IsNullOrWhiteSpace(name));
            }
            rv = Db.SQL<RawView>("select rw from rawview rw where fullname = ?", 
                "QueryProcessingTest.Company").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.Inherits != null);
            Trace.Assert(rv.Inherits.Name == "Agent");
        }

        public static void TestRuntimeColumnMetadata() {
#if false // TODO RUS:
            Column c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is rawview", 
                "materializedcolumn").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "MaterializedColumn");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.RawView);
            Trace.Assert(c.Type.Name == "materialized_column");
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).FullName == "materialized_column");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "Column");
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.Column");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table IS RawView", 
                "base_table").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "base_table");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.RawView);
            Trace.Assert(c.Type.Name == "materialized_table");
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).FullName == "materialized_table");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "materialized_table");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits == null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is RawView and inherited = ?",
                "inherits", false).First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Inherits");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.RawView);
            Trace.Assert(c.Type.Name == "Table");
            Trace.Assert((c.Type as Table).FullName == "Starcounter.Metadata.Table");
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Metadata.RawView).FullName == (c.Type as Table).FullName);
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.Table");
            Trace.Assert(c.Table.Name == "Table");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is RawView and inherited = ?",
                "uniqueIdentifierReversed", false).First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "UniqueIdentifierReversed");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.DbPrimitiveType);
            Trace.Assert(c.Type.Name == "string");
            Trace.Assert((c.Type as Starcounter.Metadata.DbPrimitiveType).PrimitiveType == sccoredb.STAR_TYPE_STRING);
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "Table");
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.Table");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            int nrColumns = 0;
            foreach (Column tc in Db.SQL<Column>(
                "select c from starcounter.metadata.column c, rawview v where c.Table = v and v.updatable = ?", false)) {
                Trace.Assert(tc.Type != null);
                if (tc.Type is Starcounter.Metadata.RawView)
                    Trace.Assert((tc.Type as Starcounter.Metadata.RawView).FullName == 
                        (tc.Type as Table).FullName);
                else {
                    Trace.Assert(tc.Type is Starcounter.Metadata.DbPrimitiveType);
                    Trace.Assert((tc.Type as Starcounter.Metadata.DbPrimitiveType).PrimitiveType != sccoredb.STAR_TYPE_REFERENCE);
                }
                Trace.Assert(tc.Table != null);
                Trace.Assert(tc.Table is RawView);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(!tc.Unique);
                nrColumns++;
            }
            Trace.Assert(nrColumns == 101);
            nrColumns = 0;
            foreach (Column tc in Db.SQL<Column>("select c from starcounter.metadata.column c where c.Table is RawView")) {
                Trace.Assert(tc.Type != null);
                if (tc.Type is Starcounter.Metadata.DbPrimitiveType)
                    Trace.Assert((tc.Type as Starcounter.Metadata.DbPrimitiveType).PrimitiveType != sccoredb.STAR_TYPE_REFERENCE);
                Trace.Assert(tc.Table != null);
                Trace.Assert(tc.Table is RawView);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(!tc.Unique);
                RawView rw = tc.Table as RawView;
                Trace.Assert(rw.UniqueIdentifierReversed.ReverseOrderDotWords() == rw.UniqueIdentifier);
                nrColumns++;
            }
            Trace.Assert(nrColumns == 212);
            Starcounter.Internal.Metadata.MaterializedIndex i = 
                Db.SQL<Starcounter.Internal.Metadata.MaterializedIndex>("select i from materializedindex i where name = ?",
                "MemberPrimaryKey").First;
            Trace.Assert(i != null);
            Index idx = Db.SQL<Index>("select i from starcounter.metadata.\"index\" i where i.table.name = ?", "VersionSource").First;
            Trace.Assert(idx != null);
            IndexedColumn idxc = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".table.name = ? and i.column.name = ?",
                "account", "accountid").First;
            Trace.Assert(idxc != null);
            var indexedColumnEnum = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".table.name = ? and i.\"index\".name = ? order by i.\"position\"",
                "materializedtable", "built-in").GetEnumerator();
            Trace.Assert(!indexedColumnEnum.MoveNext());
            indexedColumnEnum.Dispose();
            indexedColumnEnum = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".name = ? order by \"position\"",
                "ColumnPrimaryKey").GetEnumerator();
            Trace.Assert(!indexedColumnEnum.MoveNext());
            indexedColumnEnum.Dispose();

            // Test that all MaterializedColumn instances are referenced from Column instances
            foreach (MaterializedColumn mc in Db.SQL<MaterializedColumn>(
                "select c from materializedColumn c where name <> ?and inherited = ?", "__id", false)) {
                Column col = Db.SQL<Column>("select c from column c where materializedcolumn = ? and c.table is RawView and inherited = ?",
                    mc, false).First;
                Trace.Assert(col != null);
                Trace.Assert(col.MaterializedColumn.Equals(mc));
                Trace.Assert(col.Name == mc.Name);
                Trace.Assert(col.Table.FullName == mc.Table.Name);
            }
#endif
        }

        public static void ClrMetadatTest() {
            int nrCc = 0;
            int nrcc = 0;
            foreach (ClrClass v in Db.SQL<ClrClass>("select c from ClrClass c where fullname LIKE ?", 
                "%commonclass")) {
                nrCc++;
                if (v.FullName == "commonclass")
                    nrcc++;
            }
            Trace.Assert(nrCc == 7);
            Trace.Assert(nrcc == 1);
            nrCc = 0;
            nrcc = 0;
            foreach (ClrClass v in Db.SQL<ClrClass>("select c from ClrClass c where name = ?",
                "commonclass")) {
                nrCc++;
                if (v.Name == "commonclass")
                    nrcc++;
            }
            Trace.Assert(nrCc == 4);
            Trace.Assert(nrcc == 2);
#if false // TODO RUS:
            MappedProperty c = Db.SQL<MappedProperty>("select c from mappedproperty c where name = ? and c.table is ClrClass", 
                "UserIdNr").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "UserIdNr");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "User");
            Trace.Assert(c.Table.FullName == "QueryProcessingTest.User");
            Trace.Assert(c.Table is ClrClass);
            ClrClass cl = c.Table as ClrClass;
            Trace.Assert(cl.FullName == "QueryProcessingTest.User");
            Trace.Assert(c.Table.UniqueIdentifierReversed == cl.FullName.ReverseOrderDotWords());
            Trace.Assert(c.Table.UniqueIdentifier == cl.FullName);
            Trace.Assert(c.DataType != null);
            Trace.Assert(c.DataType is Starcounter.Metadata.ClrPrimitiveType);
            Trace.Assert((c.DataType as Starcounter.Metadata.ClrPrimitiveType).DbTypeCode == (ushort)DbTypeCode.Int32);
            Trace.Assert(c.DataType.Name == "Int32");
            Trace.Assert((c.Table as ClrClass).AssemblyName == "QueryProcessingTest");
            c = Db.SQL<MappedProperty>("select c from starcounter.metadata.MappedProperty c where name = ? and c.table is ClrClass", 
                "WriteLoss").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "WriteLoss");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "ClrPrimitiveType");
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.Table is ClrClass);
            Trace.Assert((c.Table as ClrClass).FullName == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.DataType != null);
            Trace.Assert(c.DataType is Starcounter.Metadata.ClrPrimitiveType);
            Trace.Assert((c.DataType as Starcounter.Metadata.ClrPrimitiveType).DbTypeCode == (ushort)DbTypeCode.Boolean);
            Trace.Assert(c.DataType.Name == "Boolean");
            Trace.Assert(String.IsNullOrEmpty((c.Table as ClrClass).AssemblyName));
            Trace.Assert((c.Table as ClrClass).AppDomainName == "sccode.exe");
            c = Db.SQL<MappedProperty>("select c from starcounter.metadata.MappedProperty c where name = ? and c.table is ClrClass", 
                "Client").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Client");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "Account");
            Trace.Assert(c.Table.FullName == "QueryProcessingTest.Account");
            Trace.Assert(c.Table is ClrClass);
            Trace.Assert((c.Table as ClrClass).FullName == "QueryProcessingTest.Account");
            Trace.Assert(c.DataType != null);
            Trace.Assert(!(c.DataType is Starcounter.Metadata.MapPrimitiveType));
            Trace.Assert(c.DataType is ClrClass);
            Trace.Assert((c.DataType as ClrClass).Name == "User");
            Trace.Assert((c.DataType as ClrClass).FullName == "QueryProcessingTest.User");
            Trace.Assert((c.DataType as ClrClass).FullName == "QueryProcessingTest.User");
            nrcc = 0;
            foreach (MappedProperty tc in Db.SQL<MappedProperty>(
                "select c from starcounter.metadata.MappedProperty c where name = ? and c.table is ClrClass", 
                "DecimalProperty")) {
                nrcc++;
                Trace.Assert(tc.Name == "DecimalProperty");
                Trace.Assert(tc.Table != null);
                Trace.Assert(tc.Table is ClrClass);
                Trace.Assert((tc.Table as ClrClass).AssemblyName == "QueryProcessingTest");
                Trace.Assert((tc.Table as ClrClass).AppDomainName == "sccode.exe");
                Trace.Assert(tc.DataType != null);
                Trace.Assert(tc.DataType is Starcounter.Metadata.ClrPrimitiveType);
                Trace.Assert((tc.DataType as Starcounter.Metadata.ClrPrimitiveType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
            }
            Trace.Assert(nrcc == 7);
            nrcc = 0;
#endif
            foreach (Column tc in Db.SQL<Column>("select c from starcounter.metadata.column c where name = ?", 
                "DecimalProperty")) {
                nrcc++;
                Trace.Assert(tc.Name == "DecimalProperty");
                Trace.Assert(tc.Table != null);
                if (tc.Table is ClrClass) {
                    Trace.Assert((tc.Table as ClrClass).AssemblyName == "QueryProcessingTest");
                    Trace.Assert((tc.Table as ClrClass).AppDomainName == "sccode.exe");
                } else {
                    Trace.Assert(tc.Table is RawView);
                }
                Trace.Assert(tc.DataType != null);
                if (tc.DataType is Starcounter.Metadata.ClrPrimitiveType)
                    Trace.Assert((tc.DataType as Starcounter.Metadata.ClrPrimitiveType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
                else {
                    Trace.Assert(tc.DataType is Starcounter.Metadata.DbPrimitiveType);
                    Trace.Assert((tc.DataType as Starcounter.Metadata.DbPrimitiveType).PrimitiveType == sccoredb.STAR_TYPE_DECIMAL);
                }
#if false // TODO RUS:
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                if (tc.Table is Starcounter.Metadata.RawView)
                    Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.Table as Starcounter.Metadata.RawView).MaterializedTable));
                else {
                    Trace.Assert(tc.Table is Starcounter.Metadata.ClrClass);
                    Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.Table as Starcounter.Metadata.ClrClass).Mapper.MaterializedTable));
                }
#endif
            }
            Trace.Assert(nrcc == 9);
        }

        public static void TestRuntimeIndexBasedOnMat() {
#if false // TODO RUS:
            int nrIndexes = 0;
            Int64 nrIndColumns = 0;
            foreach (MaterializedIndex matIndx in Db.SQL<MaterializedIndex>(
                "select i from materializedindex i")) {
                    if (Db.SQL("select t from rawview t where updatable = ? and materializedtable = ?", 
                        true, matIndx.Table).First != null) {
                        nrIndexes++;
                        int count = 0;
                        Index indx = null;
                        foreach (Index i in Db.SQL<Index>(
                            "select i from \"index\" i where i.table.fullname = ? and i.name = ?",
                            matIndx.Table.Name, matIndx.Name)) {
                            count++;
                            indx = i;
                        }
                        Trace.Assert(count == 1);
                        Trace.Assert(indx != null);
                        Int64 numMatColIndx = Db.SQL<Int64>("select count(c) from MaterializedIndexColumn c where c.\"index\" = ?",
                            matIndx).First;
                        Trace.Assert(numMatColIndx > 0);
                        Int64 numColIndx = Db.SQL<Int64>("select count(c) from indexedcolumn c where c.\"index\" = ?",
                            indx).First;
                        Trace.Assert(numColIndx == numMatColIndx);
                        Int64 numMatchedColIndx = Db.SQL<Int64>(
                            "select count(c) from indexedcolumn c, materializedindexcolumn m where c.\"index\" = ? and m.\"index\" = ?" +
                            " and c.column.name = m.column.name",
                            indx, matIndx).First;
                        Trace.Assert(numMatchedColIndx == numColIndx);
                        nrIndColumns += numColIndx;
                    }
            }
            Trace.Assert(nrIndexes == 39);
            Trace.Assert(nrIndColumns == 42);
#endif
        }

        public static void TestRuntimeIndex() {
            Int64 nrIndexes = 0;
            Int64 nrIndColumns = 0;
            foreach (Index i in Db.SQL<Index>("select i from \"index\" i")) {
                nrIndexes++;
                nrIndColumns += Db.SQL<Int64>("select count(c) from indexedcolumn c where \"index\" = ?", i).First;
            }
            Trace.Assert(nrIndexes == 28);
            Trace.Assert(nrIndColumns == 36);
            Trace.Assert(nrIndColumns == Db.SlowSQL<Int64>(
                "select count(*) from indexedcolumn").First);
        }
    }
}
