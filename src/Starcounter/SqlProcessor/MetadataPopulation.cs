﻿using Starcounter.Binding;
using Starcounter.Internal;
using Starcounter.Metadata;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Starcounter.SqlProcessor {
    public static class MetadataPopulation {
        internal static void PopulateClrViewsMetaData(TypeDef[] typeDefs) {
            Db.SystemTransaction(delegate {
                ClrView[] createdViews = new ClrView[typeDefs.Length];
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
                    ClrView parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrView>("select v from clrview v where fullclassname = ?", typeDef.BaseName).First;
                    ClrView obj = new ClrView {
                        Name = typeDef.Name.LastDotWord(),
                        FullClassName = typeDef.Name,
                        FullNameReversed = fullNameRev,
                        FullName = fullName,
                        MaterializedTable = mattab,
                        AssemblyName = (app != null ? app.Name : null),
                        AppdomainName = AppDomain.CurrentDomain.FriendlyName,
                        ParentTable = parentView,
                        Updatable = app == null ? false : true
                    };
                    createdViews[j] = obj;
                }
                // Insert meta-data about properties
                for (int j = 0; j < typeDefs.Length; j++) {
                    TypeDef typeDef = typeDefs[j];
                    ClrView theView = createdViews[j];
                    Debug.Assert(theView.FullClassName == typeDef.Name);
                    for (int i = 0; i < typeDef.PropertyDefs.Length; i++) {
                        PropertyDef propDef = typeDef.PropertyDefs[i];
                        BaseType propType = null;
                        if (propDef.Type == DbTypeCode.Object)
                            propType = Db.SQL<ClrView>("select v from clrview v where fullclassname = ?", propDef.TargetTypeName).First;
                        else
                            propType = Db.SQL<MappedType>("select t from mappedtype t where dbtypecode = ?", propDef.Type).First;
                        if (propType != null) {
                            if (propDef.ColumnName == null) {
                                CodeProperty codeProp = new CodeProperty {
                                    BaseTable = theView,
                                    Name = propDef.Name,
                                    Type = propType
                                };
                            } else {
                                MaterializedColumn matCol = Db.SQL<MaterializedColumn>(
                                    "select c from materializedcolumn c where table = ? and name = ?",
                                    theView.MaterializedTable, propDef.ColumnName).First;
                                TableColumn col = new TableColumn {
                                    BaseTable = theView,
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
                Name = typeDef.TableDef.Name.LastDotWord(),
                MaterializedTable = matTab,
                ParentTable = parentTab,
                Updatable = true
            };
            rawView.FullNameReversed = GetFullName(matTab.Name);
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
            Debug.Assert(thisType.ParentTable == null || thisType.ParentTable is RawView);
            if ((thisType.ParentTable == null) || (thisType.ParentTable as RawView).MaterializedTable.Name != typeDef.TableDef.BaseName) {
                // The parent was changed
                RawView newParent = null;
                if (typeDef.TableDef.BaseName != null) {
                    newParent = Db.SQL<RawView>("select v from rawview v where materializedtable.name = ?",
                        typeDef.TableDef.BaseName).First;
                    Debug.Assert(newParent != null);
                }
                thisType.ParentTable = newParent;
            }
            RemoveTableColumnInstances(thisType);
        }

        internal static void RemoveTableColumnInstances(RawView thisView) {
            Debug.Assert(thisView != null);
            foreach(TableColumn t in Db.SQL<TableColumn>(
                "select t from tablecolumn t where t.basetable = ?", thisView)) {
                    Debug.Assert(t.BaseTable.Equals(thisView));
                    t.Delete();
            }
        }
        internal static void CreateTableColumnInstances(TypeDef typeDef) {
            RawView thisView = Db.SQL<RawView>("select v from rawview v where materializedtable.name =?",
        typeDef.TableDef.Name).First;
            Debug.Assert(thisView != null);
            for (int i = 1; i < typeDef.TableDef.ColumnDefs.Length;i++ ) {
                ColumnDef col = typeDef.TableDef.ColumnDefs[i];
                MaterializedColumn matCol = Db.SQL<MaterializedColumn>(
                    "select c from materializedcolumn c where name = ? and table = ?",
                    col.Name, thisView.MaterializedTable).First;
                Debug.Assert(matCol != null);
                TableColumn newCol = new TableColumn {
                    BaseTable = thisView,
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
                    newCol.Type = Db.SQL<MaterializedType>(
                        "select t from materializedtype t where primitivetype = ?",
                        col.Type).First;
                Debug.Assert(newCol.Type != null);
            }
        }
    }
}
