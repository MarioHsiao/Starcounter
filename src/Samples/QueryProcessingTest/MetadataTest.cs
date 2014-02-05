using System;
using System.Diagnostics;
using Starcounter;
using Starcounter.Internal;
using Starcounter.Metadata;

namespace QueryProcessingTest {
    public static class MetadataTest {
        public static void TestPopulatedMetadata() {
            HelpMethods.LogEvent("Test populated meta-data");
            TestTypeMetadata();
            TestRuntimeColumnMetadata();
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
            Trace.Assert(count == 10);
            Trace.Assert(acc == 55);

            MappedType mapt = Db.SQL<MappedType>("select t from mappedtype t where name = ?", "Int16").First;
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
            Trace.Assert(count == 16);
            MaterializedTable m = Db.SQL<MaterializedTable>("select m from MaterializedTable m where name = ?", "BaseType").First;
            Trace.Assert(m != null);
            Trace.Assert(m.BaseTable == null);
            Trace.Assert(m.Name == "BaseType");
            MaterializedColumn c = Db.SQL<MaterializedColumn>("select c from materializedcolumn c where name = ?", "parenttable").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Table.Name == "BaseTable");
            RawView rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "BaseType").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullName == "BaseType.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.ParentTable == null);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "ClrView").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullName == "ClrView.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.ParentTable != null);
            Trace.Assert(rv.ParentTable.Name == "VMView");
            Trace.Assert(rv.ParentTable.ParentTable != null);
            Trace.Assert(rv.ParentTable.ParentTable.ParentTable != null);
            Trace.Assert(rv.ParentTable.ParentTable.ParentTable.Name == "BaseTable");
            Trace.Assert(rv.ParentTable.ParentTable.ParentTable.ParentTable != null);
            Trace.Assert(rv.ParentTable.ParentTable.ParentTable.ParentTable.Name == "BaseType");
            Trace.Assert(rv.ParentTable.ParentTable.ParentTable.ParentTable.ParentTable == null);
            count = 0;
            foreach (RawView v in Db.SQL<RawView>("select rv from rawView rv")) {
                Trace.Assert(v.MaterializedTable != null);
                Trace.Assert(v.MaterializedTable.Name == v.Name);
                count++;
            }
            Trace.Assert(count == 16);
            rv = Db.SQL<RawView>("select rw from rawview rw where name = ?", "materialized_index").First;
            Trace.Assert(rv != null);
            Trace.Assert(rv.FullName == "materialized_index.Raw.Starcounter");
            Trace.Assert(rv.MaterializedTable != null);
            Trace.Assert(rv.MaterializedTable.Name == rv.Name);
            Trace.Assert(!rv.Updatable);
            Trace.Assert(rv.ParentTable == null);
        }

        public static void TestRuntimeColumnMetadata() {
            TableColumn c = Db.SQL<TableColumn>("select c from TableColumn c where name = ?", "materializedcolumn").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "MaterializedColumn");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is HostMaterializedTable);
            Trace.Assert(c.Type.Name == "materialized_column");
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable.Name == "materialized_column");
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "TableColumn");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).ParentTable != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<TableColumn>("select c from TableColumn c where name = ?", "base_table").First;
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
            Trace.Assert((c.BaseTable as RawView).ParentTable == null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<TableColumn>("select c from TableColumn c where name = ?", "parenttable").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "ParentTable");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is HostMaterializedTable);
            Trace.Assert(c.Type.Name == "BaseTable");
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable != null);
            Trace.Assert((c.Type as HostMaterializedTable).MaterializedTable.Name == c.Type.Name);
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "BaseTable");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).ParentTable != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            c = Db.SQL<TableColumn>("select c from TableColumn c where name = ?", "fullName").First;
            Trace.Assert(c != null);
            Trace.Assert(c.Name == "FullName");
            Trace.Assert(c.Type != null);
            Trace.Assert(c.Type is MaterializedType);
            Trace.Assert(c.Type.Name == "string");
            Trace.Assert((c.Type as MaterializedType).PrimitiveType == sccoredb.STAR_TYPE_STRING);
            Trace.Assert(c.BaseTable != null);
            Trace.Assert(c.BaseTable.Name == "BaseTable");
            Trace.Assert(c.BaseTable is RawView);
            Trace.Assert((c.BaseTable as RawView).ParentTable != null);
            Trace.Assert(c.MaterializedColumn != null);
            Trace.Assert(c.MaterializedColumn.Name == c.Name);
            Trace.Assert(!c.Unique);
            int nrColumns = 0;
            foreach (TableColumn tc in Db.SQL<TableColumn>("select c from Tablecolumn c")) {
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
            Trace.Assert(nrColumns == 20 + 18);
            MaterializedIndex i = Db.SQL<MaterializedIndex>("select i from materializedindex i where name = ?",
                "TableColumnPrimaryKey").First;
            Trace.Assert(i != null);
        }
    }
}
