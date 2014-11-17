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
            Db.SystemTransaction(delegate {
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
                    Starcounter.Internal.Metadata.MaterializedTable mattab =
                        Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>("select m from materializedtable m where name = ?",
                        typeDef.TableDef.Name).First;
                    ClrClass parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrClass>("select v from ClrClass v where fullclassname = ?", typeDef.BaseName).First;
                    ClrClass obj = new ClrClass {
                        Name = typeDef.Name.LastDotWord(),
                        FullName = typeDef.Name,
                        FullClassName = typeDef.Name,
                        UniqueIdentifierReversed = uniqueIdentifierRev,
                        UniqueIdentifier = uniqueIdentifier,
                        MaterializedTable = mattab,
                        AssemblyName = (app != null ? app.Name : null),
                        AppDomainName = AppDomain.CurrentDomain.FriendlyName,
                        Inherits = parentView,
                        Updatable = app == null ? false : true
                    };
                    createdViews[j] = obj;
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
                            propType = Db.SQL<ClrClass>("select v from ClrClass v where fullclassname = ?", propDef.TargetTypeName).First;
                        else
                            propType = Db.SQL<Starcounter.Metadata.MapPrimitiveType>("select t from MapPrimitivetype t where dbtypecode = ?", propDef.Type).First;
                        if (propType != null) {
                            if (propDef.ColumnName == null) {
                                CodeProperty codeProp = new CodeProperty {
                                    Table = theView,
                                    Name = propDef.Name,
                                    Type = propType
                                };
                            } else {
                                Starcounter.Internal.Metadata.MaterializedColumn matCol =
                                    Db.SQL<Starcounter.Internal.Metadata.MaterializedColumn>(
                                    "select c from materializedcolumn c where c.table = ? and name = ?",
                                    theView.MaterializedTable, propDef.ColumnName).First;
                                Column col = new Column {
                                    Table = theView,
                                    Name = propDef.Name,
                                    MaterializedColumn = matCol,
                                    Type = propType,
                                    Unique = false
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
                "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            Debug.Assert(Db.SQL<RawView>("select v from rawview v where materializedtable = ?",
                matTab).First == null);
            RawView parentTab = Db.SQL<RawView>(
                "select v from rawview v where fullname = ?", typeDef.TableDef.BaseName).First;
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
            Debug.Assert(Db.SQL<ClrClass>("select v from clrclass v where v.Fullname = ?",
                typeDef.TableDef.Name).First == null); // Always dropped and new created
            RawView thisType = Db.SQL<RawView>("select v from rawview v where UniqueIdentifier = ?",
                GetUniqueIdentifier(typeDef.TableDef.Name)).First;
            Debug.Assert(thisType != null);
            Debug.Assert(thisType.MaterializedTable == null);
            Starcounter.Internal.Metadata.MaterializedTable matTab = Db.SQL<Starcounter.Internal.Metadata.MaterializedTable>(
                "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            thisType.MaterializedTable = matTab;
            Debug.Assert(thisType.Inherits == null || thisType.Inherits is RawView);
            if ((thisType.Inherits == null) || (thisType.Inherits as RawView).FullName != typeDef.TableDef.BaseName) {
                // The parent was changed
                RawView newParent = null;
                if (typeDef.TableDef.BaseName != null) {
                    newParent = Db.SQL<RawView>("select v from rawview v where fullname = ?",
                        typeDef.TableDef.BaseName).First;
                    Debug.Assert(newParent != null);
                }
                thisType.Inherits = newParent;
            }
            RemoveColumnInstances(thisType);
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
            foreach (RawView inherited in Db.SQL<RawView>("select v from rawview v where inherits = ?", rawView)) {
                Debug.Assert(Db.SQL("select materializedtable from rawview v where v = ?", inherited).First == null);
                Debug.Assert(inherited.MaterializedTable == null);
                MaterializedTable t = Db.SQL<MaterializedTable>(
                    "select t from materializedtable t where name = ?", inherited.FullName).First;
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
            RawView thisView = Db.SQL<RawView>("select v from rawview v where fullname =?",
        typeDef.TableDef.Name).First;
            Debug.Assert(thisView != null);
            for (int i = 0; i < typeDef.TableDef.ColumnDefs.Length; i++) {
                ColumnDef col = typeDef.TableDef.ColumnDefs[i];
                Starcounter.Internal.Metadata.MaterializedColumn matCol = Db.SQL<Starcounter.Internal.Metadata.MaterializedColumn>(
                    "select c from materializedcolumn c where c.name = ? and c.table = ?",
                    col.Name, thisView.MaterializedTable).First;
                Debug.Assert(matCol != null);
                Column newCol = new Column {
                    Table = thisView,
                    Name = matCol.Name,
                    MaterializedColumn = matCol
                };
                if (col.Type == sccoredb.STAR_TYPE_REFERENCE) {
                    PropertyDef prop = typeDef.PropertyDefs[0];
                    for (int j = 1; prop.ColumnName != col.Name && j < typeDef.PropertyDefs.Length; j++) {
                        prop = typeDef.PropertyDefs[j];
                    }
                    if (prop.ColumnName == col.Name)
                        newCol.Type = Db.SQL<RawView>("select v from rawview v where fullname = ?",
                            prop.TargetTypeName).First;
                } else
                    newCol.Type = Db.SQL<Starcounter.Metadata.DbPrimitiveType>(
                        "select t from DbPrimitiveType t where primitivetype = ?",
                        col.Type).First;
            }
        }

        internal static void CreateAnIndexInstance(MaterializedIndex matIndx) {
            Debug.Assert(matIndx != null);
            Index rawIndx = new Index {
                //MaterializedIndex = matIndx,
                Name = matIndx.Name,
                Table =
                    Db.SQL<RawView>("select v from rawview v where materializedtable = ?", matIndx.Table).First,
                Unique = matIndx.Unique
            };
            Debug.Assert(rawIndx.Table != null);
            Debug.Assert(rawIndx.Table is Starcounter.Metadata.RawView);
            //Debug.Assert((rawIndx.Table as Starcounter.Metadata.RawView).MaterializedTable.Equals(rawIndx.MaterializedIndex.Table));
            foreach (MaterializedIndexColumn matCol in Db.SQL<MaterializedIndexColumn>(
                "select c from MaterializedIndexColumn c where \"index\" = ?", matIndx)) {
                //Debug.Assert(matCol.Index.Equals(rawIndx.MaterializedIndex));
                IndexedColumn rawColIndx = new IndexedColumn {
                    Ascending =
                        matCol.Order == 0,
                    Column =
                        Db.SQL<Column>("select c from column c where c.table = ? and materializedcolumn = ?",
                        rawIndx.Table, matCol.Column).First,
                    Index =
                        rawIndx,
                    //MaterializedIndexColumn = matCol,
                    Position = matCol.Place
                };
                Debug.Assert(rawColIndx.Column != null);
            }
            Debug.Assert(Db.SQL("select c from indexedColumn c where \"index\" = ?", rawIndx).First != null);
        }

        internal static void CreateIndexInstances(TypeDef typeDef) {
            foreach (MaterializedIndex matIndx in Db.SQL<MaterializedIndex>
                ("select i from materializedIndex i where tableid = ?", typeDef.TableDef.TableId)) {
                if (Db.SQL<Index>(
                    "select i from \"index\" i, rawview v where i.table  = v and v.MaterializedTable = ?  and i.name = ?",
                    matIndx.Table, matIndx.Name).First == null)
                    CreateAnIndexInstance(matIndx);
            }
        }
    }
}
