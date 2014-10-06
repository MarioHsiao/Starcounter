using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;
using Starcounter.Binding;

namespace QueryProcessingTest {
    public static class MetadataTest {
        public static void TestPopulatedMetadata() {
            HelpMethods.LogEvent("Test populated meta-data");
            TestTypeMetadata();
            TestRuntimeColumnMetadata();
            ClrMetadatTest();
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
            Starcounter.Internal.Metadata.MaterializedTable m = 
                Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>("select m from MaterializedTable m where name = ?", 
                "Starcounter.Metadata.Type").First;
            Trace.Assert(m != null);
            Trace.Assert(m.BaseTable == null);
            Trace.Assert(m.Name == "Starcounter.Metadata.Type");
            Starcounter.Internal.Metadata.MaterializedColumn c = 
                Db.SQL<Starcounter.Internal.Metadata.MaterializedColumn>("select c from materializedcolumn c where name = ? and c.table.name = ?",
                "Inherits", "starcounter.metadata.table").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Table.Name == "Starcounter.Metadata.Table");
            count = 0;
            foreach (Starcounter.Internal.Metadata.MaterializedColumn mc in 
                Db.SQL<Starcounter.Internal.Metadata.MaterializedColumn>("select c from materializedcolumn c where name = ?",
                "inherits")) {
                count++;
                }
            Trace.Assert(count == 5);
            RawView rv = Db.SQL<RawView>("select rw from rawview rw where fullname = ?", 
                "Starcounter.Metadata.Type").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.UniqueIdentifierReversed == "Type.Metadata.Starcounter.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            //Trace.Assert(rv.MaterializedTable.Name == rv.FullName);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits == null);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "ClrClass").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.UniqueIdentifierReversed == "ClrClass.Metadata.Starcounter.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            //Trace.Assert(rv.MaterializedTable.Name == rv.FullName);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits != null);
            Trace.Assert(rv.Inherits.FullName == "Starcounter.Metadata.VMView");
            Trace.Assert(rv.Inherits.Name == "VMView");
            Trace.Assert(rv.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits.FullName == "Starcounter.Metadata.Table");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Name == "Table");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits.FullName == "Starcounter.Metadata.Type");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits.Name == "Type");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits.Inherits == null);
            count = 0;
            foreach (RawView v in Db.SQL<RawView>("select rv from rawView rv")) {
                Trace.Assert(v.MaterializedTable != null);
                //Trace.Assert(v.MaterializedTable.Name == v.FullName);
                count++;
            }
            Trace.Assert(count == 42);
            count = 0;
            foreach (RawView v in Db.SQL<RawView>("select rv from rawView rv where updatable = ?", 
                false)) {
                Trace.Assert(v.MaterializedTable != null);
                //Trace.Assert(v.MaterializedTable.Name == v.FullName);
                Trace.Assert(v.UniqueIdentifier == v.UniqueIdentifierReversed.ReverseOrderDotWords());
                Trace.Assert(!String.IsNullOrWhiteSpace(v.Name));
                count++;
            }
            Trace.Assert(count == 18);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", 
                "materialized_index").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.UniqueIdentifierReversed == "materialized_index.Raw.Starcounter");
            Trace.Assert(rv.UniqueIdentifier == rv.UniqueIdentifierReversed.ReverseOrderDotWords());
            Trace.Assert(rv.MaterializedTable != null);
            //Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits == null);
            count = 0;
            foreach (Starcounter.Metadata.MapPrimitiveType mt in 
                Db.SQL<Starcounter.Metadata.MapPrimitiveType>("select t from MapPrimitiveType t")) {
                Starcounter.Binding.DbTypeCode typeCode = (Starcounter.Binding.DbTypeCode)mt.DbTypeCode;
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
        }

        public static void TestRuntimeColumnMetadata() {
            Column c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is rawview", 
                "materializedcolumn").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "MaterializedColumn");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Internal.Metadata.HostMaterializedTable);
            Trace.Assert(c.Type.Name == "materialized_column");
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).FullName == "materialized_column");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "Column");
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.Column");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ?", 
                "base_table").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "base_table");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Internal.Metadata.HostMaterializedTable);
            Trace.Assert(c.Type.Name == "materialized_table");
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).FullName == "materialized_table");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "materialized_table");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits == null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is RawView",
                "inherits").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Inherits");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Internal.Metadata.HostMaterializedTable);
            Trace.Assert(c.Type.Name == "Table");
            Trace.Assert((c.Type as Table).FullName == "Starcounter.Metadata.Table");
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as Starcounter.Internal.Metadata.HostMaterializedTable).FullName == (c.Type as Table).FullName);
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.Table");
            Trace.Assert(c.Table.Name == "Table");
            Trace.Assert(c.Table is RawView);
            Trace.Assert((c.Table as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.Table is RawView",
                "uniqueIdentifierReversed").First;
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
                if (tc.Type is Starcounter.Internal.Metadata.HostMaterializedTable)
                    Trace.Assert((tc.Type as Starcounter.Internal.Metadata.HostMaterializedTable).FullName == 
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
            Trace.Assert(nrColumns == 30 + 20);
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
            Trace.Assert(nrColumns == 116);
            Starcounter.Internal.Metadata.MaterializedIndex i = 
                Db.SQL<Starcounter.Internal.Metadata.MaterializedIndex>("select i from materializedindex i where name = ?",
                "ColumnPrimaryKey").First;
            Trace.Assert(i != null);
            Index idx = Db.SQL<Index>("select i from starcounter.metadata.\"index\" i where i.\"table\".name = ?", "VersionSource").First;
            Trace.Assert(idx == null);
            IndexedColumn idxc = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".\"table\".name = ? and i.column.name = ?",
                "account", "accountid").First;
            Trace.Assert(idxc == null);
            var indexedColumnEnum = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".\"table\".name = ? and i.\"index\".name = ? order by i.\"position\"",
                "materializedtable", "built-in").GetEnumerator();
            Trace.Assert(!indexedColumnEnum.MoveNext());
            indexedColumnEnum = Db.SQL<IndexedColumn>(
                "select i from indexedcolumn i where i.\"index\".name = ? order by \"position\"",
                "ColumnPrimaryKey").GetEnumerator();
            Trace.Assert(!indexedColumnEnum.MoveNext());
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
            Column c = Db.SQL<Column>("select c from column c where name = ? and c.table is ClrClass", 
                "UserIdNr").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "UserIdNr");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "User");
            Trace.Assert(c.Table.FullName == "QueryProcessingTest.User");
            Trace.Assert(c.Table is ClrClass);
            ClrClass cl = c.Table as ClrClass;
            Trace.Assert(cl.FullClassName == "QueryProcessingTest.User");
            Trace.Assert(c.Table.UniqueIdentifierReversed == cl.FullClassName.ReverseOrderDotWords());
            Trace.Assert(c.Table.UniqueIdentifier == cl.FullClassName);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.Table as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.Table as ClrClass).FullClassName);
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.MapPrimitiveType);
            Trace.Assert((c.Type as Starcounter.Metadata.MapPrimitiveType).DbTypeCode == (ushort)DbTypeCode.Int32);
            Trace.Assert(c.Type.Name == "Int32");
            Trace.Assert((c.Table as ClrClass).AssemblyName == "QueryProcessingTest");
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.table is ClrClass", 
                "WriteLoss").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "WriteLoss");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "ClrPrimitiveType");
            Trace.Assert(c.Table.FullName == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.Table is ClrClass);
            Trace.Assert((c.Table as ClrClass).FullClassName == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.Table as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.Table as ClrClass).FullName);
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is Starcounter.Metadata.MapPrimitiveType);
            Trace.Assert((c.Type as Starcounter.Metadata.MapPrimitiveType).DbTypeCode == (ushort)DbTypeCode.Boolean);
            Trace.Assert(c.Type.Name == "Boolean");
            Trace.Assert(String.IsNullOrEmpty((c.Table as ClrClass).AssemblyName));
            Trace.Assert((c.Table as ClrClass).AppDomainName == "sccode.exe");
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.table is ClrClass", 
                "Client").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Client");
            Trace.Assert(c.Table != null);
            Trace.Assert(c.Table.Name == "Account");
            Trace.Assert(c.Table.FullName == "QueryProcessingTest.Account");
            Trace.Assert(c.Table is ClrClass);
            Trace.Assert((c.Table as ClrClass).FullClassName == "QueryProcessingTest.Account");
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.Table as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.Table as ClrClass).FullClassName);
            Trace.Assert(c.Type != null);
            Trace.Assert(!(c.Type is Starcounter.Metadata.MapPrimitiveType));
            Trace.Assert(c.Type is ClrClass);
            Trace.Assert((c.Type as ClrClass).Name == "User");
            Trace.Assert((c.Type as ClrClass).FullName == "QueryProcessingTest.User");
            Trace.Assert((c.Type as ClrClass).FullClassName == "QueryProcessingTest.User");
            nrcc = 0;
            foreach (Column tc in Db.SQL<Column>(
                "select c from starcounter.metadata.column c where name = ? and table is ClrClass", 
                "DecimalProperty")) {
                nrcc++;
                Trace.Assert(tc.Name == "DecimalProperty");
                Trace.Assert(tc.Table != null);
                Trace.Assert(tc.Table is ClrClass);
                Trace.Assert((tc.Table as ClrClass).AssemblyName == "QueryProcessingTest");
                Trace.Assert((tc.Table as ClrClass).AppDomainName == "sccode.exe");
                Trace.Assert(tc.Type != null);
                Trace.Assert(tc.Type is Starcounter.Metadata.MapPrimitiveType);
                Trace.Assert((tc.Type as Starcounter.Metadata.MapPrimitiveType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.Table as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable));
                Trace.Assert(tc.MaterializedColumn.Table.Name == (tc.Table as ClrClass).FullClassName);
            }
            Trace.Assert(nrcc == 7);
            nrcc = 0;
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
                Trace.Assert(tc.Type != null);
                if (tc.Type is Starcounter.Metadata.MapPrimitiveType)
                    Trace.Assert((tc.Type as Starcounter.Metadata.MapPrimitiveType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
                else {
                    Trace.Assert(tc.Type is Starcounter.Metadata.DbPrimitiveType);
                    Trace.Assert((tc.Type as Starcounter.Metadata.DbPrimitiveType).PrimitiveType == sccoredb.STAR_TYPE_DECIMAL);
                }
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.Table as Starcounter.Internal.Metadata.HostMaterializedTable).MaterializedTable));
            }
            Trace.Assert(nrcc == 7*2);
        }
    }
}
