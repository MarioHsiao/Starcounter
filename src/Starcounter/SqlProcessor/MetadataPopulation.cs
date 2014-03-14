using Starcounter.Binding;
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
                    string fullName = classReverseFullName + assemblyName + '.' + AppDomain.CurrentDomain.FriendlyName;
                    MaterializedTable mattab = Db.SQL<MaterializedTable>("select m from materializedtable m where name = ?",
                        typeDef.TableDef.Name).First;
                    ClrView parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrView>("select v from clrview v where fullclassname = ?", typeDef.BaseName).First;
                    ClrView obj = new ClrView {
                        Name = typeDef.Name.LastDotWord(),
                        FullClassName = typeDef.Name,
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

        internal static void CreateRawTableInstance(TypeDef typeDef) {
            MaterializedTable matTab = Db.SQL<MaterializedTable>(
                "select t from materializedtable t where name = ?", typeDef.TableDef.Name).First;
            RawView parentTab = Db.SQL<RawView>(
                "select v from rawview v where name = ?", typeDef.TableDef.BaseName).First;
            Debug.Assert(matTab != null);
            RawView rawView = new RawView {
                Name = typeDef.TableDef.Name.LastDotWord(),
                MaterializedTable = matTab,
                ParentTable = parentTab,
                Updatable = true
            };
            rawView.FullName = rawView.Name.ReverseOrderDotWords() + ".Raw.Starcounter";
        }

        internal static void UpgradeRawTableInstance(TypeDef typeDef) { }
        internal static void RemoveTableColumnInstances(TypeDef typeDef) { }
        internal static void CreateTableColumnInstances(TypeDef typeDef) { }
    }
}
