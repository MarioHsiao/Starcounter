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
            MaterializedType t = Db.SQL<MaterializedType>("select t from materializedType t order by primitivetype").First;
            Trace.Assert(t != null);
            Trace.Assert(t.Name == "string");
            Trace.Assert(t.PrimitiveType == sccoredb.STAR_TYPE_STRING);
            ulong acc = 0;
            int count = 0;
            foreach (MaterializedType mt in Db.SQL<MaterializedType>("select t from materializedType t")) {
                acc += mt.PrimitiveType;
                count++;
            }
            Trace.Assert(count == 9);
            Trace.Assert(acc == 45);
            MappedType mapt = Db.SQL<MappedType>("select t from mappedtype t where name = ?", 
                "Int16").First;
            Trace.Assert(mapt != null);
            Trace.Assert(mapt is ClrPrimitiveType);
            Trace.Assert(mapt.MaterializedType.Name == "long");
            Trace.Assert(!mapt.WriteLoss);
            Trace.Assert(mapt.ReadLoss);
            acc = 0;
            count = 0;
            foreach (MappedType mpt in Db.SQL<MappedType>("select t from mappedtype t")) {
                Trace.Assert(mpt.MaterializedType != null);
                count++;
            }
            Trace.Assert(count == 15);
            MaterializedTable m = Db.SQL<MaterializedTable>("select m from MaterializedTable m where name = ?", 
                "Type").First;
            Trace.Assert(m != null);
            Trace.Assert(m.BaseTable == null);
            Trace.Assert(m.Name == "Type");
            MaterializedColumn c = Db.SQL<MaterializedColumn>("select c from materializedcolumn c where name = ? and c.table.name = ?",
                "Inherits", "table").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Table.Name == "Table");
            count = 0;
            foreach(MaterializedColumn mc in Db.SQL<MaterializedColumn>("select c from materializedcolumn c where name = ?",
                "inherits")) {
                count++;
                }
            Trace.Assert(count == 5);
            RawView rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", 
                "Type").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullNameReversed == "Type.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits == null);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "ClrClass").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullNameReversed == "ClrClass.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits != null);
            Trace.Assert(rv.Inherits.Name == "VMView");
            Trace.Assert(rv.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits.Name == "Table");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits != null);
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits.Name == "Type");
            Trace.Assert(rv.Inherits.Inherits.Inherits.Inherits.Inherits == null);
            count = 0;
            foreach (RawView v in Db.SQL<RawView>("select rv from rawView rv")) {
                Trace.Assert(v.MaterializedTable != null);
                Trace.Assert(v.MaterializedTable.Name == v.Name);
                count++;
            }
            Trace.Assert(count == 38);
            count = 0;
            foreach (RawView v in Db.SQL<RawView>("select rv from rawView rv where updatable = ?", 
                false)) {
                Trace.Assert(v.MaterializedTable != null);
                Trace.Assert(v.MaterializedTable.Name == v.Name);
                Trace.Assert(v.FullName == v.FullNameReversed.ReverseOrderDotWords());
                count++;
            }
            Trace.Assert(count == 16);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", 
                "materialized_index").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullNameReversed == "materialized_index.Raw.Starcounter");
            Trace.Assert(rv.FullName == rv.FullNameReversed.ReverseOrderDotWords());
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.Inherits == null);
            count = 0;
            foreach (MappedType mt in Db.SQL<MappedType>("select t from mappedtype t")) {
                Starcounter.Binding.DbTypeCode typeCode = (Starcounter.Binding.DbTypeCode)mt.DbTypeCode;
                Trace.Assert(mt.Name == Enum.GetName(typeof(Starcounter.Binding.DbTypeCode), typeCode));
                count++;
            }
            Trace.Assert(count == 15);
        }

        public static void TestRuntimeColumnMetadata() {
            Column c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.BaseTable is rawview", 
                "materializedcolumn").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "MaterializedColumn");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is HostMaterializedTable);
            Trace.Assert(c.Type.Name == "materialized_column");
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable.Name == "materialized_column");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "Column");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ?", 
                "base_table").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "base_table");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is HostMaterializedTable);
            Trace.Assert(c.Type.Name == "materialized_table");
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable.Name == "materialized_table");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "materialized_table");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).Inherits == null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.BaseTable is RawView",
                "inherits").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Inherits");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is HostMaterializedTable);
            Trace.Assert(c.Type.Name == "Table");
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable.Name == c.Type.Name);
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "Table");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.BaseTable is RawView", 
                "fullNameReversed").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "FullNameReversed");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is MaterializedType);
            Trace.Assert(c.Type.Name == "string");
            Trace.Assert((c.Type as MaterializedType).PrimitiveType == sccoredb.STAR_TYPE_STRING);
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "Table");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).Inherits != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            int nrColumns = 0;
            foreach (Column tc in Db.SQL<Column>(
                "select c from starcounter.metadata.column c, rawview v where c.BaseTable = v and v.updatable = ?", false)) {
                Trace.Assert(tc.Type != null);
                if (tc.Type is HostMaterializedTable)
                    Trace.Assert((tc.Type as HostMaterializedTable).MaterializedTable.Name == tc.Type.Name);
                else {
                    Trace.Assert(tc.Type is MaterializedType);
                    Trace.Assert((tc.Type as MaterializedType).PrimitiveType != sccoredb.STAR_TYPE_REFERENCE);
                }
                Trace.Assert(tc.BaseTable != null);
                Trace.Assert(tc.BaseTable is RawView);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(!tc.Unique);
                nrColumns++;
            }
            Trace.Assert(nrColumns == 20 + 20);
            nrColumns = 0;
            foreach (Column tc in Db.SQL<Column>("select c from starcounter.metadata.column c where c.BaseTable is RawView")) {
                Trace.Assert(tc.Type != null);
                if (tc.Type is MaterializedType)
                    Trace.Assert((tc.Type as MaterializedType).PrimitiveType != sccoredb.STAR_TYPE_REFERENCE);
                Trace.Assert(tc.BaseTable != null);
                Trace.Assert(tc.BaseTable is RawView);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(!tc.Unique);
                RawView rw = tc.BaseTable as RawView;
                Trace.Assert(rw.FullNameReversed.ReverseOrderDotWords() == rw.FullName);
                nrColumns++;
            }
            Trace.Assert(nrColumns == 103);
            MaterializedIndex i = Db.SQL<MaterializedIndex>("select i from materializedindex i where name = ?",
                "ColumnPrimaryKey").First;
            Trace.Assert(i != null);
        }

        public static void ClrMetadatTest() {
            int nrCc = 0;
            int nrcc = 0;
            foreach (ClrClass v in Db.SQL<ClrClass>("select c from ClrClass c where name LIKE ?", 
                "%commonclass")) {
                nrCc++;
                if (v.Name == "commonclass")
                    nrcc++;
            }
            Trace.Assert(nrCc == 7);
            Trace.Assert(nrcc == 1);
            Column c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.basetable is ClrClass", 
                "UserIdNr").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "UserIdNr");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "QueryProcessingTest.User");
            Trace.Assert(c.BaseTable is ClrClass);
            ClrClass cl = c.BaseTable as ClrClass;
            Trace.Assert(cl.FullClassName == "QueryProcessingTest.User");
            Trace.Assert(c.BaseTable.FullNameReversed == cl.FullClassName.ReverseOrderDotWords() + "." + 
                (cl.AssemblyName == null ? "" : cl.AssemblyName + ".") + cl.AppdomainName);
            Trace.Assert(c.BaseTable.FullName == cl.AppdomainName + "." + 
                (cl.AssemblyName == null ? "" : cl.AssemblyName + ".") + cl.FullClassName);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.BaseTable as HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.BaseTable as ClrClass).FullClassName);
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is MappedType);
            Trace.Assert((c.Type as MappedType).DbTypeCode == (ushort)DbTypeCode.Int32);
            Trace.Assert(c.Type.Name == "Int32");
            Trace.Assert((c.BaseTable as ClrClass).AssemblyName == "QueryProcessingTest");
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.basetable is ClrClass", 
                "WriteLoss").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "WriteLoss");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.BaseTable is ClrClass);
            Trace.Assert((c.BaseTable as ClrClass).FullClassName == "Starcounter.Metadata.ClrPrimitiveType");
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.BaseTable as HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.BaseTable as ClrClass).Name.LastDotWord());
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is MappedType);
            Trace.Assert((c.Type as MappedType).DbTypeCode == (ushort)DbTypeCode.Boolean);
            Trace.Assert(c.Type.Name == "Boolean");
            Trace.Assert(String.IsNullOrEmpty((c.BaseTable as ClrClass).AssemblyName));
            Trace.Assert((c.BaseTable as ClrClass).AppdomainName == "sccode.exe");
            c = Db.SQL<Column>("select c from starcounter.metadata.column c where name = ? and c.basetable is ClrClass", 
                "Client").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "Client");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "QueryProcessingTest.Account");
            Trace.Assert(c.BaseTable is ClrClass);
            Trace.Assert((c.BaseTable as ClrClass).FullClassName == "QueryProcessingTest.Account");
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(c.MaterializedColumn.Table.Equals((c.BaseTable as HostMaterializedTable).MaterializedTable));
            Trace.Assert(c.MaterializedColumn.Table.Name == (c.BaseTable as ClrClass).FullClassName);
            Trace.Assert(c.Type != null);
            Trace.Assert(!(c.Type is MappedType));
            Trace.Assert(c.Type is ClrClass);
            Trace.Assert((c.Type as ClrClass).Name == "QueryProcessingTest.User");
            Trace.Assert((c.Type as ClrClass).FullClassName == "QueryProcessingTest.User");
            nrcc = 0;
            foreach (Column tc in Db.SQL<Column>(
                "select c from starcounter.metadata.column c where name = ? and basetable is ClrClass", 
                "DecimalProperty")) {
                nrcc++;
                Trace.Assert(tc.Name == "DecimalProperty");
                Trace.Assert(tc.BaseTable != null);
                Trace.Assert(tc.BaseTable is ClrClass);
                Trace.Assert((tc.BaseTable as ClrClass).AssemblyName == "QueryProcessingTest");
                Trace.Assert((tc.BaseTable as ClrClass).AppdomainName == "sccode.exe");
                Trace.Assert(tc.Type != null);
                Trace.Assert(tc.Type is MappedType);
                Trace.Assert((tc.Type as MappedType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.BaseTable as HostMaterializedTable).MaterializedTable));
                Trace.Assert(tc.MaterializedColumn.Table.Name == (tc.BaseTable as ClrClass).FullClassName);
            }
            Trace.Assert(nrcc == 7);
            nrcc = 0;
            foreach (Column tc in Db.SQL<Column>("select c from starcounter.metadata.column c where name = ?", 
                "DecimalProperty")) {
                nrcc++;
                Trace.Assert(tc.Name == "DecimalProperty");
                Trace.Assert(tc.BaseTable != null);
                if (tc.BaseTable is ClrClass) {
                    Trace.Assert((tc.BaseTable as ClrClass).AssemblyName == "QueryProcessingTest");
                    Trace.Assert((tc.BaseTable as ClrClass).AppdomainName == "sccode.exe");
                } else {
                    Trace.Assert(tc.BaseTable is RawView);
                }
                Trace.Assert(tc.Type != null);
                if (tc.Type is MappedType) 
                Trace.Assert((tc.Type as MappedType).DbTypeCode == (UInt16)DbTypeCode.Decimal);
                else {
                    Trace.Assert(tc.Type is MaterializedType);
                    Trace.Assert((tc.Type as MaterializedType).PrimitiveType == sccoredb.STAR_TYPE_DECIMAL);
                }
                Trace.Assert(tc.MaterializedColumn != null);
                Trace.Assert(tc.MaterializedColumn.Name == tc.Name);
                Trace.Assert(tc.MaterializedColumn.Table.Equals((tc.BaseTable as HostMaterializedTable).MaterializedTable));
            }
            Trace.Assert(nrcc == 7*2);
        }
    }
}
