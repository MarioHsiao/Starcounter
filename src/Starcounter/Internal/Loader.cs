
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Sc.Server.Weaver.Schema;
using Starcounter.Internal.Weaver;

namespace Starcounter.Internal
{

    internal static class LoaderHelper
    {

        internal static void MapPropertyDefsToColumnDefs(ColumnDef[] columnDefs, PropertyDef[] propertyDefs)
        {
            for (int pi = 0; pi < propertyDefs.Length; pi++)
            {
                var columnName = propertyDefs[pi].ColumnName;
                if (columnName != null)
                {
                    try
                    {
                        int ci = 0;
                        for (; ; )
                        {
                            if (columnDefs[ci].Name == columnName)
                            {
                                propertyDefs[pi].ColumnIndex = ci;
                                break;
                            }
                            ci++;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new Exception(); // TODO:
                    }
                }
            }
        }
    }

    public static class SchemaLoader
    {

        private const string rootClassName = "Starcounter.Entity";

        public static List<TypeDef> LoadAndConvertSchema(DirectoryInfo inputDir)
        {
            var schemaFiles = inputDir.GetFiles("*.schema");

            var databaseSchema = new DatabaseSchema();
            databaseSchema.AddStarcounterAssembly();

            var typeDefs = new List<TypeDef>();

            DatabaseAssembly databaseAssembly;

            for (int i = 0; i < schemaFiles.Length; i++)
            {
                databaseAssembly = DatabaseAssembly.Deserialize(schemaFiles[i].FullName);
                databaseSchema.Assemblies.Add(databaseAssembly);
            }

            databaseSchema.AfterDeserialization();

            var databaseClasses = new List<DatabaseEntityClass>();
            databaseSchema.PopulateDatabaseEntityClasses(databaseClasses);
            
            var databaseClassCount = databaseClasses.Count;
            for (int i = 0; i < databaseClassCount; i++)
            {
                var databaseClass = databaseClasses[i];
                databaseAssembly = databaseClass.Assembly;
                var assemblyName = new AssemblyName(databaseAssembly.FullName);
                var typeLoader = new TypeLoader(assemblyName, databaseClass.Name);
                typeDefs.Add(EntityClassToTypeDef(databaseClass, typeLoader));
            }

            return typeDefs;
        }

        private static TypeDef EntityClassToTypeDef(DatabaseEntityClass databaseClass, TypeLoader typeLoader)
        {
            var columnDefs = new List<ColumnDef>();
            var propertyDefs = new List<PropertyDef>();

            GatherColumnAndPropertyDefs(databaseClass, columnDefs, propertyDefs, false);
            var columnDefArray = columnDefs.ToArray();
            var propertyDefArray = propertyDefs.ToArray();
            LoaderHelper.MapPropertyDefsToColumnDefs(columnDefArray, propertyDefArray);

            string baseName = databaseClass.BaseClass == null ? null : databaseClass.BaseClass.Name;
            if (baseName == rootClassName) baseName = null;

            var tableDef = new TableDef(databaseClass.Name, baseName, columnDefArray);
            var typeDef = new TypeDef(databaseClass.Name, baseName, propertyDefArray, typeLoader, tableDef);

            return typeDef;
        }

        private static void GatherColumnAndPropertyDefs(DatabaseEntityClass databaseClass, List<ColumnDef> columnDefs, List<PropertyDef> propertyDefs, bool subClass)
        {
            var baseDatabaseClass = databaseClass.BaseClass as DatabaseEntityClass;
            if (baseDatabaseClass != null)
            {
                GatherColumnAndPropertyDefs(baseDatabaseClass, columnDefs, propertyDefs, true);
            }

            var databaseAttributes = databaseClass.Attributes;

            for (int i = 0; i < databaseAttributes.Count; i++)
            {
                var databaseAttribute = databaseAttributes[i];

                DbTypeCode type;
                string targetTypeName;

                var databaseAttributeType = databaseAttribute.AttributeType;

                DatabasePrimitiveType databasePrimitiveType;
                DatabaseEnumType databaseEnumType;
                DatabaseEntityClass databaseEntityClass;

                if ((databasePrimitiveType = databaseAttributeType as DatabasePrimitiveType) != null)
                {
                    type = PrimitiveToTypeCode(databasePrimitiveType.Primitive);
                    targetTypeName = null;
                }
                else if ((databaseEnumType = databaseAttributeType as DatabaseEnumType) != null)
                {
                    type = PrimitiveToTypeCode(databaseEnumType.UnderlyingType);
                    targetTypeName = null;
                }
                else if ((databaseEntityClass = databaseAttributeType as DatabaseEntityClass) != null)
                {
                    type = DbTypeCode.Object;
                    targetTypeName = databaseEntityClass.Name;
                }
                else
                {
                    if (!databaseAttribute.IsPersistent) continue;

                    // Persistent attribute needs to be of a type supported by
                    // the database.

                    throw new NotSupportedException(); // TODO:
                }

                var isNullable = databaseAttribute.IsNullable;

                // Fix handling that always nullable types are correcly
                // tagged as nullable in the schema file.

                switch (type)
                {
                    case DbTypeCode.Object:
                    case DbTypeCode.String:
                    case DbTypeCode.Binary:
                    case DbTypeCode.LargeBinary:
                        isNullable = true;
                        break;
                }

                switch (databaseAttribute.AttributeKind)
                {
                case DatabaseAttributeKind.PersistentField:
                    columnDefs.Add(new ColumnDef(
                        databaseAttribute.Name,
                        type,
                        isNullable,
                        subClass
                        ));

                    if (databaseAttribute.IsPublicRead)
                    {
                       var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );
                        propertyDef.ColumnName = databaseAttribute.Name;
                        propertyDefs.Add(propertyDef);
                    }
                    break;
                case DatabaseAttributeKind.PersistentProperty:
                    if (databaseAttribute.IsPublicRead)
                    {
                        var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );
                        propertyDef.ColumnName = databaseAttribute.PersistentProperty.AttributeFieldIndex;
                        propertyDefs.Add(propertyDef);
                    }
                    break;
                case DatabaseAttributeKind.NotPersistentProperty:
                    if (databaseAttribute.IsPublicRead)
                    {
                        var propertyDef = new PropertyDef(
                            databaseAttribute.Name,
                            type,
                            isNullable,
                            targetTypeName
                            );

                        string columnName = null;
                        var backingField = databaseAttribute.BackingField;
                        if (backingField != null && backingField.AttributeKind == DatabaseAttributeKind.PersistentField)
                        {
                            columnName = backingField.Name;
                        }
                        propertyDef.ColumnName = columnName;

                        propertyDefs.Add(propertyDef);
                    }
                    break;
                }
            }
        }

        private static DbTypeCode PrimitiveToTypeCode(DatabasePrimitive primitive)
        {
            switch (primitive)
            {
            case DatabasePrimitive.Boolean:
                return DbTypeCode.Boolean;
            case DatabasePrimitive.Byte:
                return DbTypeCode.Byte;
            case DatabasePrimitive.DateTime:
                return DbTypeCode.DateTime;
            case DatabasePrimitive.Decimal:
                return DbTypeCode.Decimal;
            case DatabasePrimitive.Double:
                return DbTypeCode.Double;
            case DatabasePrimitive.Int16:
                return DbTypeCode.Int16;
            case DatabasePrimitive.Int32:
                return DbTypeCode.Int32;
            case DatabasePrimitive.Int64:
                return DbTypeCode.Int64;
            case DatabasePrimitive.SByte:
                return DbTypeCode.SByte;
            case DatabasePrimitive.Single:
                return DbTypeCode.Single;
            case DatabasePrimitive.String:
                return DbTypeCode.String;
            case DatabasePrimitive.TimeSpan:
                throw new NotSupportedException();
            case DatabasePrimitive.UInt16:
                return DbTypeCode.UInt16;
            case DatabasePrimitive.UInt32:
                return DbTypeCode.UInt32;
            case DatabasePrimitive.UInt64:
                return DbTypeCode.UInt64;
            case DatabasePrimitive.Binary:
                return DbTypeCode.Binary;;
            case DatabasePrimitive.LargeBinary:
                return DbTypeCode.LargeBinary;
            case DatabasePrimitive.None:
            default:
                throw new NotSupportedException();
            }
        }
    }
}
