using Starcounter.Binding;
using Starcounter.Internal;
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
                    string assemblyName = "";
                    Application app = Application.CurrentAssigned;
                    if (app != null) {
                        //string assemblyPath = app.FilePath;
                        //assemblyName = '.' + assemblyPath.Substring(assemblyPath.LastIndexOf('\\'));
                        assemblyName = '.' + app.Name;
                    }
                    string fullNameRev = classReverseFullName + assemblyName + '.' + AppDomain.CurrentDomain.FriendlyName;
                    string fullName = AppDomain.CurrentDomain.FriendlyName + assemblyName + '.' + typeDef.Name;
                    MaterializedTable mattab = Db.SQL<MaterializedTable>("select m from materializedtable m where name = ?",
                        typeDef.TableDef.Name).First;
                    ClrClass parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrClass>("select v from ClrClass v where fullclassname = ?", typeDef.BaseName).First;
                    ClrClass obj = new ClrClass {
                        Name = typeDef.Name,
                        FullClassName = typeDef.Name,
                        FullNameReversed = fullNameRev,
                        FullName = fullName,
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
                            propType = Db.SQL<Starcounter.Internal.Metadata.MappedType>("select t from mappedtype t where dbtypecode = ?", propDef.Type).First;
                        if (propType != null) {
                            if (propDef.ColumnName == null) {
                                CodeProperty codeProp = new CodeProperty {
                                    Table = theView,
                                    Name = propDef.Name,
                                    Type = propType
                                };
                            } else {
                                MaterializedColumn matCol = Db.SQL<MaterializedColumn>(
                                    "select c from materializedcolumn c where table = ? and name = ?",
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

        internal static string GetFullName(string tableName) {
            return "Starcounter.Raw." + tableName;
        }

        internal static string GetFullNameReversed(string tableName) {
            return tableName.ReverseOrderDotWords() + ".Raw.Starcounter";
        }

        internal static void CreateRawTableInstance(TypeDef typeDef) {
            MaterializedTable matTab = Db.SQL<MaterializedTable>(
                "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            Debug.Assert(Db.SQL<RawView>("select v from rawview v where materializedtable = ?",
                matTab).First == null);
            RawView parentTab = Db.SQL<RawView>(
                "select v from rawview v where name = ?", typeDef.TableDef.BaseName).First;
            RawView rawView = new RawView {
                Name = typeDef.TableDef.Name,
                MaterializedTable = matTab,
                Inherits = parentTab,
                Updatable = true
            };
            rawView.FullName = GetFullName(matTab.Name);
            rawView.FullNameReversed = GetFullNameReversed(matTab.Name);
        }

        internal static void UpgradeRawTableInstance(TypeDef typeDef) {
            Debug.Assert(Db.SQL<RawView>("select v from rawview v where v.materializedtable.name = ?",
                typeDef.TableDef.Name).First == null); // Always dropped and new created
            RawView thisType = Db.SQL<RawView>("select v from rawview v where fullname = ?",
                GetFullName(typeDef.TableDef.Name)).First;
            Debug.Assert(thisType != null);
            Debug.Assert(thisType.MaterializedTable == null);
            MaterializedTable matTab = Db.SQL<MaterializedTable>(
                "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
            Debug.Assert(matTab != null);
            thisType.MaterializedTable = matTab;
            Debug.Assert(thisType.Inherits == null || thisType.Inherits is RawView);
            if ((thisType.Inherits == null) || (thisType.Inherits as RawView).MaterializedTable.Name != typeDef.TableDef.BaseName) {
                // The parent was changed
                RawView newParent = null;
                if (typeDef.TableDef.BaseName != null) {
                    newParent = Db.SQL<RawView>("select v from rawview v where materializedtable.name = ?",
                        typeDef.TableDef.BaseName).First;
                    Debug.Assert(newParent != null);
                }
                thisType.Inherits = newParent;
            }
            RemoveColumnInstances(thisType);
        }

        internal static void RemoveColumnInstances(RawView thisView) {
            Debug.Assert(thisView != null);
            foreach(Column t in Db.SQL<Column>(
                "select t from starcounter.metadata.column t where t.table = ?", thisView)) {
                    Debug.Assert(t.Table.Equals(thisView));
                    t.Delete();
            }
        }
        internal static void CreateColumnInstances(TypeDef typeDef) {
            RawView thisView = Db.SQL<RawView>("select v from rawview v where materializedtable.name =?",
        typeDef.TableDef.Name).First;
            Debug.Assert(thisView != null);
            for (int i = 1; i < typeDef.TableDef.ColumnDefs.Length;i++ ) {
                ColumnDef col = typeDef.TableDef.ColumnDefs[i];
                MaterializedColumn matCol = Db.SQL<MaterializedColumn>(
                    "select c from materializedcolumn c where name = ? and table = ?",
                    col.Name, thisView.MaterializedTable).First;
                Debug.Assert(matCol != null);
                Column newCol = new Column {
                    Table = thisView,
                    Name = matCol.Name,
                    MaterializedColumn = matCol
                };
                if (col.Type == sccoredb.STAR_TYPE_REFERENCE) {
                    PropertyDef prop = typeDef.PropertyDefs[0];
                    for (int j = 1; prop.ColumnName != col.Name; j++) {
                        Debug.Assert(j < typeDef.PropertyDefs.Length);
                        prop = typeDef.PropertyDefs[j];
                    }
                    newCol.Type = Db.SQL<RawView>("select v from rawview v where materializedtable.name = ?",
                        prop.TargetTypeName).First;
                } else
                    newCol.Type = Db.SQL<Starcounter.Internal.Metadata.MaterializedType>(
                        "select t from materializedtype t where primitivetype = ?",
                        col.Type).First;
                Debug.Assert(newCol.Type != null);
            }
        }
    }
}
