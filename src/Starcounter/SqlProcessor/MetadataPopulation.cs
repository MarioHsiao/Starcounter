﻿using Starcounter.Binding;
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
                        Db.SQL<Starcounter.Metadata.RawView>("select m from rawview m where fullname = ?",
                        typeDef.TableDef.Name).First;
                    Debug.Assert(rawview != null);
                    ClrClass parentView = null;
                    if (typeDef.BaseName != null)
                        parentView = Db.SQL<ClrClass>("select v from ClrClass v where fullclassname = ?", typeDef.BaseName).First;
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
                        Starcounter.Metadata.DataType propType = null;
                        if (propDef.Type == DbTypeCode.Object)
                            propType = Db.SQL<ClrClass>("select v from ClrClass v where fullclassname = ?", propDef.TargetTypeName).First;
                        else
                            propType = Db.SQL<Starcounter.Metadata.ClrPrimitiveType>("select t from ClrPrimitivetype t where dbtypecode = ?", propDef.Type).First;
                        if (propType != null) {
                            if (propDef.ColumnName == null) {
                                CodeProperty codeProp = new CodeProperty {
                                    Table = theView,
                                    Name = propDef.Name,
                                    DataType = propType,
                                    Get = true,
                                    Set = true
                                };
                            } else {
                                Starcounter.Internal.Metadata.RawColumn rawCol =
                                    Db.SQL<Starcounter.Internal.Metadata.RawColumn>(
                                    "select c from starcounter.internal.metadata.rawcolumn c where c.table = ? and name = ?",
                                    theView.Mapper, propDef.ColumnName).First;
                                Debug.Assert(rawCol != null);
                                MappedProperty prop = new MappedProperty {
                                    Table = theView,
                                    Column = rawCol,
                                    Inherited = rawCol.Inherited,
                                    Name = propDef.Name,
                                    DataType = propType,
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

    }
}
