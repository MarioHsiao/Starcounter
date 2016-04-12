using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Internal.Metadata;
using Starcounter.Metadata;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public static class MetadataPopulation {
        internal static void PopulateClrMetadata(TypeDef[] typeDefs) {
            Db.SystemTransact(delegate {
                ClrClass[] createdViews = new ClrClass[typeDefs.Length];
                // Insert meta-data about types
                for (int j = 0; j < typeDefs.Length; j++) {
                    TypeDef typeDef = typeDefs[j];
                    string classReverseFullName = typeDef.Name.ReverseOrderDotWords();
                    //string assemblyName = "";
                    Application app = Application.CurrentAssigned;
                    //if (app != null) {
                    //    //string assemblyPath = app.FilePath;
                    //    //assemblyName = '.' + assemblyPath.Substring(assemblyPath.LastIndexOf('\\'));
                    //    assemblyName = '.' + app.Name;
                    //}
                    string uniqueIdentifierRev = classReverseFullName;
                    string uniqueIdentifier = typeDef.Name;
                    Starcounter.Metadata.RawView rawview =
                        Db.SQL<Starcounter.Metadata.RawView>("select m from Starcounter.Metadata.rawview m where fullname = ?",
                        typeDef.TableDef.Name).First;
                    Debug.Assert(rawview != null);
                    ClrClass parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrClass>("select v from Starcounter.Metadata.ClrClass v where fullclassname = ?", typeDef.BaseName).First;
                    ClrClass obj = new ClrClass {
                        Name = typeDef.Name.LastDotWord(),
                        FullName = typeDef.Name,
                        FullClassName = typeDef.Name,
                        UniqueIdentifierReversed = uniqueIdentifierRev,
                        UniqueIdentifier = uniqueIdentifier,
                        Mapper = rawview,
                        AssemblyName = (app != null ? app.Name : null),
                        AppDomainName = AppDomain.CurrentDomain.FriendlyName,
                        Inherits = parentView,
                        Updatable = app == null ? false : true
                    };
                    createdViews[j] = obj;
                    Debug.Assert(obj.Mapper != null);
                }
                // Insert meta-data about properties
                for (int j = 0; j < typeDefs.Length; j++) {
                    TypeDef typeDef = typeDefs[j];
                    ClrClass theView = createdViews[j];
                    Debug.Assert(theView.FullClassName == typeDef.Name);
                    for (int i = 0; i < typeDef.PropertyDefs.Length; i++) {
                        PropertyDef propDef = typeDef.PropertyDefs[i];
                        Starcounter.Metadata.Type propType = null;
                        if (propDef.Type == DbTypeCode.Object)
                            propType = Db.SQL<ClrClass>("select v from Starcounter.Metadata.ClrClass v where fullclassname = ?", propDef.TargetTypeName).First;
                        else
                            propType = Db.SQL<Starcounter.Metadata.ClrPrimitiveType>("select t from Starcounter.Metadata.ClrPrimitivetype t where dbtypecode = ?", propDef.Type).First;
                        if (propType != null) {
                            if (propDef.ColumnName == null) {
                                CodeProperty codeProp = new CodeProperty {
                                    Table = theView,
                                    Name = propDef.Name,
                                    Type = propType,
                                    Get = true,
                                    Set = true
                                };
                            } else {
                                Starcounter.Metadata.Column rawCol =
                                    Db.SQL<Starcounter.Metadata.Column>(
                                    "select c from Starcounter.Metadata.Column c where c.table = ? and name = ?",
                                    theView.Mapper, propDef.ColumnName).First;
                                if (rawCol == null)
                                    throw ErrorCode.ToException(Error.SCERRUNEXPMETADATA,
                                        "Unexpecably not found metadata for column " +
                                        propDef.ColumnName + " in table " + 
                                        theView.Mapper.FullName);
                                MappedProperty prop = new MappedProperty {
                                    Table = theView,
                                    Column = rawCol,
                                    Inherited = rawCol.Inherited,
                                    Name = propDef.Name,
                                    Type = propType,
                                    Set = true,
                                    Get = true
                                };
                            }
                        } else {
                            LogSources.Sql.LogWarning("Non database type " +
                                (propDef.Type == DbTypeCode.Object ? propDef.TargetTypeName : propDef.Type.ToString()) +
                                " of property " + propDef.Name + " in class " + theView.FullClassName);
                        }
                    }
                }
            });
        }

        internal static string GetUniqueIdentifier(string tableName) {
            return "Starcounter.Raw." + tableName;
        }

        internal static string GetFullNameReversed(string tableName) {
            return tableName.ReverseOrderDotWords() + ".Raw.Starcounter";
        }

        internal static void CreateRawTableInstance(TypeDef typeDef) {
            Starcounter.Internal.Metadata.MaterializedTable matTab = Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>(
                "select t from Starcounter.Internal.Metadata.materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            Debug.Assert(Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where materializedtable = ?",
                matTab).First == null);
            RawView parentTab = Db.SQL<RawView>(
                "select v from Starcounter.Metadata.rawview v where fullname = ?", typeDef.TableDef.BaseName).First;
            Debug.Assert(matTab.BaseTable == null && parentTab == null ||
                matTab.BaseTable != null && parentTab != null && matTab.BaseTable.Equals(parentTab.MaterializedTable));
            RawView rawView = new RawView {
                Name = typeDef.TableDef.Name.LastDotWord(),
                FullName = typeDef.TableDef.Name,
                MaterializedTable = matTab,
                Inherits = parentTab,
                Updatable = true
            };
            rawView.UniqueIdentifier = GetUniqueIdentifier(matTab.Name);
            rawView.UniqueIdentifierReversed = GetFullNameReversed(matTab.Name);
        }

        internal static void UpgradeRawTableInstance(TypeDef typeDef) {
            Debug.Assert(Db.SQL<ClrClass>("select v from Starcounter.Metadata.clrclass v where v.Fullname = ?",
                typeDef.TableDef.Name).First == null); // Always dropped and new created
            RawView thisType = Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where UniqueIdentifier = ?",
                GetUniqueIdentifier(typeDef.TableDef.Name)).First;
            Debug.Assert(thisType != null);
            Debug.Assert(thisType.MaterializedTable == null);
            Starcounter.Internal.Metadata.MaterializedTable matTab = Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>(
                "select t from Starcounter.Internal.Metadata.materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            thisType.MaterializedTable = matTab;
            Debug.Assert(thisType.Inherits == null || thisType.Inherits is RawView);
            if ((thisType.Inherits == null) || (thisType.Inherits as RawView).FullName != typeDef.TableDef.BaseName) {
                // The parent was changed
                RawView newParent = null;
                if (typeDef.TableDef.BaseName != null) {
                    newParent = Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where fullname = ?",
                        typeDef.TableDef.BaseName).First;
                    Debug.Assert(newParent != null);
                }
                thisType.Inherits = newParent;
            }
            UpgradeInheritedRawTableInstance(thisType);
        }

        /// <summary>
        /// When a table is upgraded, MaterializedTable instances are reset to 
        /// new instances for all inherited tables.
        /// Therefore RawTable instances representing inherited tables should be
        /// updated to the correct MaterializedTable instance.
        /// </summary>
        /// <param name="typeDef"></param>
        /// <param name="rawView"></param>
        internal static void UpgradeInheritedRawTableInstance(RawView rawView) {
            RemoveColumnInstances(rawView);
            foreach (RawView inherited in Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where inherits = ?", rawView)) {
                Debug.Assert(Db.SQL("select materializedtable from Starcounter.Metadata.rawview v where v = ?", inherited).First == null);
                Debug.Assert(inherited.MaterializedTable == null);
                MaterializedTable t = Db.SQL<MaterializedTable>(
                    "select t from Starcounter.Internal.Metadata.materializedtable t where name = ?", inherited.FullName).First;
                Debug.Assert(t != null);
                inherited.MaterializedTable = t;
                // Repeat for children
                UpgradeInheritedRawTableInstance(inherited);
            }
        }

        internal static void RemoveColumnInstances(RawView thisView) {
            Debug.Assert(thisView != null);
            foreach (Column t in Db.SQL<Column>(
                "select t from starcounter.metadata.column t where t.table = ?", thisView)) {
                Debug.Assert(t.Table.Equals(thisView));
                t.Delete();
            }
        }

        internal static void CreateColumnInstances(TypeDef typeDef) {
            RawView thisView = Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where fullname =?",
        typeDef.TableDef.Name).First;
            Debug.Assert(thisView != null);
            if (Db.SQL("select c from Starcounter.Metadata.column c where c.table = ?", thisView).First != null)
                return; // We don't need to create columns for this table and inheriting, since the were created during inheritence
            for (int i = 0; i < typeDef.TableDef.ColumnDefs.Length; i++) {
                ColumnDef col = typeDef.TableDef.ColumnDefs[i];
                Starcounter.Internal.Metadata.MaterializedColumn matCol = Db.SQL<Starcounter.Internal.Metadata.MaterializedColumn>(
                    "select c from Starcounter.Internal.Metadata.materializedcolumn c where c.name = ? and c.table = ?",
                    col.Name, thisView.MaterializedTable).First;
                Debug.Assert(matCol != null);
                Column newCol = new Column {
                    Table = thisView,
                    Name = matCol.Name,
                    MaterializedColumn = matCol,
                    Inherited = col.IsInherited
                };
                if (col.Type == sccoredb.STAR_TYPE_REFERENCE) {
                    PropertyDef prop = typeDef.PropertyDefs[0];
                    for (int j = 1; prop.ColumnName != col.Name && j < typeDef.PropertyDefs.Length; j++) {
                        prop = typeDef.PropertyDefs[j];
                    }
                    if (prop.ColumnName == col.Name)
                        newCol.Type = Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where fullname = ?",
                            prop.TargetTypeName).First;
                } else
                    newCol.Type = Db.SQL<Starcounter.Metadata.DbPrimitiveType>(
                        "select t from Starcounter.Metadata.DbPrimitiveType t where primitivetype = ?",
                        col.Type).First;
                Debug.Assert(newCol.Type != null);
            }
            UpdateIndexInstances(thisView.MaterializedTable);
            // Create columns for inherited tables, since they were removed
            foreach (RawView inheritingView in Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where v.inherits = ?", thisView)) {
                TypeDef inheritingTypeDef = Bindings.GetTypeDef(inheritingView.FullName);
                if (inheritingTypeDef != null)
                    CreateColumnInstances(inheritingTypeDef);
            }
        }

        internal static void CreateAnIndexInstance(MaterializedIndex matIndx) {
            Debug.Assert(matIndx != null);
            Index rawIndx = new Index {
                //MaterializedIndex = matIndx,
                Name = matIndx.Name,
                Table =
                    Db.SQL<RawView>("select v from Starcounter.Metadata.rawview v where materializedtable = ?", matIndx.Table).First,
                Unique = matIndx.Unique
            };
            Debug.Assert(rawIndx.Table != null);
            Debug.Assert(rawIndx.Table is Starcounter.Metadata.RawView);
            //Debug.Assert((rawIndx.Table as Starcounter.Metadata.RawView).MaterializedTable.Equals(rawIndx.MaterializedIndex.Table));
            foreach (MaterializedIndexColumn matCol in Db.SQL<MaterializedIndexColumn>(
                "select c from Starcounter.Internal.Metadata.MaterializedIndexColumn c where \"index\" = ?", matIndx)) {
                //Debug.Assert(matCol.Index.Equals(rawIndx.MaterializedIndex));
                IndexedColumn rawColIndx = new IndexedColumn {
                    Ascending =
                        matCol.Order == 0,
                    Column =
                        Db.SQL<Column>("select c from Starcounter.Metadata.column c where c.table = ? and materializedcolumn = ?",
                        rawIndx.Table, matCol.Column).First,
                    Index =
                        rawIndx,
                    //MaterializedIndexColumn = matCol,
                    Position = matCol.Place
                };
                Debug.Assert(rawColIndx.Column != null);
            }
            Debug.Assert(Db.SQL("select c from Starcounter.Metadata.indexedColumn c where \"index\" = ?", rawIndx).First != null);
        }

        /// <summary>
        /// Synchronizes instances of Index with instances of MaterializedIndex.
        /// It is necessary to insert if new indexes appeared or remove if they were dropped due schema changes.
        /// It goes through children types.
        /// </summary>
        /// <param name="tableId">TableId of the type to udpate indexes.</param>
        internal static void UpdateIndexInstances(MaterializedTable matTbl) {
            foreach (MaterializedIndex matIndx in Db.SQL<MaterializedIndex>
                ("select i from starcounter.internal.metadata.materializedIndex i where i.table = ?", matTbl)) {
                Index existingIndex = Db.SQL<Index>(
                    "select i from Starcounter.Metadata.\"index\" i, Starcounter.Metadata.rawview v where i.table  = v and v.MaterializedTable = ?  and i.name = ?",
                    matIndx.Table, matIndx.Name).First;
                if (existingIndex == null)
                    CreateAnIndexInstance(matIndx);
            }
            foreach (Index indx in Db.SQL<Index>(
                "select i from starcounter.metadata.\"index\" i, starcounter.metadata.rawview v where i.table = v and v.materializedtable = ?",
                matTbl)) {
                MaterializedIndex existingIndex = Db.SQL<MaterializedIndex>(
                        "select i from Starcounter.Internal.Metadata.materializedindex i where i.table = ? and name = ?", matTbl, indx.Name).First;
                    if (existingIndex == null) {
                        foreach(IndexedColumn colIndx in Db.SQL<IndexedColumn>(
                            "select c from starcounter.metadata.indexedcolumn c where \"index\" = ?",
                            indx))
                            colIndx.Delete();
                        indx.Delete();
                    }
            }
#if DEBUG
            long nrIndx = Db.SQL<long>("select count(i) from Starcounter.Metadata.\"index\" i, Starcounter.Metadata.rawview v where i.table = v and v.materializedtable = ?", matTbl).First;
            long nrMatIndx = Db.SQL<long>("select count(i) from Starcounter.Internal.Metadata.materializedindex i where i.table = ?", matTbl).First;
#endif
            Debug.Assert(Db.SQL<long>("select count(i) from Starcounter.Metadata.\"index\" i, Starcounter.Metadata.rawview v where i.table = v and v.materializedtable = ?", matTbl).First ==
                Db.SQL<long>("select count(i) from Starcounter.Internal.Metadata.materializedindex i where i.table = ?", matTbl).First);
        }
    }
}
