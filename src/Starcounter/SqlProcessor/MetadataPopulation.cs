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
                        parentView = Db.SQL<ClrClass>("select v from Starcounter.Metadata.ClrClass v where fullname = ?", typeDef.BaseName).First;
                    ClrClass obj = new ClrClass {
                        Name = typeDef.Name.LastDotWord(),
                        FullName = typeDef.Name,
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
                    Debug.Assert(theView.FullName == typeDef.Name);
                    for (int i = 0; i < typeDef.PropertyDefs.Length; i++) {
                        PropertyDef propDef = typeDef.PropertyDefs[i];
                        Starcounter.Metadata.DataType propType = null;
                        if (propDef.Type == DbTypeCode.Object)
                            propType = Db.SQL<ClrClass>("select v from Starcounter.Metadata.ClrClass v where fullname = ?", propDef.TargetTypeName).First;
                        else
                            propType = Db.SQL<Starcounter.Metadata.ClrPrimitiveType>("select t from Starcounter.Metadata.ClrPrimitivetype t where dbtypecode = ?", propDef.Type).First;
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
                                    DataType = propType,
                                    Set = true,
                                    Get = true
                                };
                            }
                        } else {
                            LogSources.Sql.LogWarning("Non database type " +
                                (propDef.Type == DbTypeCode.Object ? propDef.TargetTypeName : propDef.Type.ToString()) +
                                " of property " + propDef.Name + " in class " + theView.FullName);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Populates CLR primitives meta-table if it is empty
        /// </summary>
        internal static void PopulateClrPrimitives() {
            Db.SystemTransact(() => {
                if (Db.SQL("SELECT p FROM Starcounter.Metadata.ClrPrimitiveType p").First != null)
                    return;
                string selectDbPrimType =
                "select t from Starcounter.Metadata.DbPrimitiveType t where PrimitiveType = ?";
                foreach (DbTypeCode dbTypeCode in Enum.GetValues(typeof(DbTypeCode))) {
                    // Ignore non-primitive and reserved type
                    if (dbTypeCode == DbTypeCode.Object || dbTypeCode == DbTypeCode.Key)
                        continue;
                    string dbTypeCodeName = Enum.GetName(typeof(DbTypeCode), dbTypeCode);
                    ClrPrimitiveType clrType = new ClrPrimitiveType {
                        Name = dbTypeCodeName,
                        DbTypeCode = (ushort)dbTypeCode
                    };
                    // Set the link to DB type
                    clrType.DbPrimitiveType =
                    Db.SQL<DbPrimitiveType>(selectDbPrimType,
                    BindingHelper.ConvertDbTypeCodeToScTypeCode(dbTypeCode)).First;
                    Debug.Assert(clrType.DbPrimitiveType != null);
                    // Set WriteLoss
                    switch (dbTypeCode) {
                        case DbTypeCode.Binary:
                        case DbTypeCode.Boolean:
                        case DbTypeCode.Byte:
                        case DbTypeCode.DateTime:
                        case DbTypeCode.Double:
                        case DbTypeCode.Int16:
                        case DbTypeCode.Int32:
                        case DbTypeCode.Int64:
                        case DbTypeCode.Key:
                        case DbTypeCode.SByte:
                        case DbTypeCode.Single:
                        case DbTypeCode.String:
                        case DbTypeCode.UInt16:
                        case DbTypeCode.UInt32:
                        case DbTypeCode.UInt64:
                            clrType.WriteLoss = false;
                            break;
                        case DbTypeCode.Decimal:
                            clrType.WriteLoss = true;
                            break;
                        default:
                            throw ErrorCode.ToException(Error.SCERRUNEXPECTEDINTERNALERROR,
                       "Unknown DbTypeCode is defined in CLR code host: " +
                       Enum.GetName(typeof(DbTypeCode), dbTypeCode));
                    }
                    // Set ReadLoss
                    switch (dbTypeCode) {
                        case DbTypeCode.Decimal:
                        case DbTypeCode.Binary:
                        case DbTypeCode.Double:
                        case DbTypeCode.Int64:
                        case DbTypeCode.Key:
                        case DbTypeCode.Single:
                        case DbTypeCode.String:
                        case DbTypeCode.UInt64:
                            clrType.ReadLoss = false;
                            break;
                        case DbTypeCode.Boolean:
                        case DbTypeCode.Byte:
                        case DbTypeCode.DateTime:
                        case DbTypeCode.Int16:
                        case DbTypeCode.Int32:
                        case DbTypeCode.SByte:
                        case DbTypeCode.UInt16:
                        case DbTypeCode.UInt32:
                            clrType.ReadLoss = true;
                            break;
                        default:
                            throw ErrorCode.ToException(Error.SCERRUNEXPECTEDINTERNALERROR,
                       "Unknown DbTypeCode is defined in CLR code host: " +
                       Enum.GetName(typeof(DbTypeCode), dbTypeCode));
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
