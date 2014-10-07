﻿// ***********************************************************************
// <copyright file="Loader.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Sc.Server.Weaver.Schema;
using Starcounter.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Starcounter.Internal
{

    /// <summary>
    /// Class LoaderHelper
    /// </summary>
    public static class LoaderHelper
    {
        /// <summary>
        /// Maps the property defs to column defs.
        /// </summary>
        /// <param name="columnDefs">The column defs.</param>
        /// <param name="propertyDefs">The property defs.</param>
        /// <param name="columnRuntimeTypes">
        /// Array parallel to columnDefs with type codes of columns as mapped by properties.
        /// </param>
        /// <exception cref="System.Exception"></exception>
        public static void MapPropertyDefsToColumnDefs(ColumnDef[] columnDefs, PropertyDef[] propertyDefs, out DbTypeCode[] columnRuntimeTypes)
        {
            DbTypeCode?[] columnRuntimeTypesTemp;
            columnRuntimeTypesTemp = new DbTypeCode?[columnDefs.Length];

            for (int pi = 0; pi < propertyDefs.Length; pi++) {
                var propertyDef = propertyDefs[pi];
                var columnName = propertyDef.ColumnName;
                if (columnName != null) {
                    try {
                        int ci = 0;
                        for (; ; ) {
                            if (columnDefs[ci].Name == columnName) {
                                propertyDef.ColumnIndex = ci;

                                // We set the type from the first property we find that maps the
                                // column. If more then one property maps the same column we assume
                                // the first one to be the primary one.

                                if (!columnRuntimeTypesTemp[ci].HasValue) {
                                    columnRuntimeTypesTemp[ci] = propertyDef.Type;
                                }
                                break;
                            }
                            ci++;
                        }
                    }
                    catch (IndexOutOfRangeException) {
                        throw ErrorCode.ToException(Error.SCERRUNEXPDBMETADATAMAPPING, "Column "+columnName+" cannot be found in ColumnDefs.");
                    }
                }
            }

            // Create final column runtime type array. For columns not mapped
            // we use the default column type for the column type.

            columnRuntimeTypes = new DbTypeCode[columnDefs.Length];
            for (var ci = 0; ci < columnRuntimeTypesTemp.Length; ci++) {
                if (columnRuntimeTypesTemp[ci].HasValue) {
                    columnRuntimeTypes[ci] = columnRuntimeTypesTemp[ci].Value;
                }
                else {
                    columnRuntimeTypes[ci] = BindingHelper.ConvertScTypeCodeToDbTypeCode(
                        columnDefs[ci].Type
                        );
                }
            }
        }
    }

    /// <summary>
    /// Class SchemaLoader
    /// </summary>
    public static class SchemaLoader
    {
        /// <summary>
        /// Loads the and convert schema.
        /// </summary>
        /// <param name="inputDir">The input dir.</param>
        /// <returns>List{TypeDef}.</returns>
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
            databaseSchema.PopulateOrderedDatabaseEntityClasses2(databaseClasses);
            
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

        /// <summary>
        /// Entities the class to type def.
        /// </summary>
        /// <param name="databaseClass">The database class.</param>
        /// <param name="typeLoader">The type loader.</param>
        /// <returns>TypeDef.</returns>
        private static TypeDef EntityClassToTypeDef(DatabaseEntityClass databaseClass, TypeLoader typeLoader)
        {
            var columnDefs = new List<ColumnDef>();
            var propertyDefs = new List<PropertyDef>();
            Boolean isObjectID = false;
            Boolean isObjectNo = false;

            string baseName = databaseClass.BaseClass == null ? null : databaseClass.BaseClass.Name;

            // Add column definition for implicit key column.
            columnDefs.Add(new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, baseName == null ? false : true));

            GatherColumnAndPropertyDefs(databaseClass, columnDefs, propertyDefs, false, ref isObjectID, ref isObjectNo);
            var columnDefArray = columnDefs.ToArray();
            var propertyDefArray = propertyDefs.ToArray();

            DbTypeCode[] columnRuntimeTypes;
            LoaderHelper.MapPropertyDefsToColumnDefs(
                columnDefArray, propertyDefArray, out columnRuntimeTypes
                );
            
            var tableDef = new TableDef(databaseClass.Name, baseName, columnDefArray);
            var typeDef = new TypeDef(
                databaseClass.Name, baseName, propertyDefArray, typeLoader, tableDef,
                columnRuntimeTypes
                );

            return typeDef;
        }

        /// <summary>
        /// Gathers the column and property defs.
        /// </summary>
        /// <param name="databaseClass">The database class.</param>
        /// <param name="columnDefs">The column defs.</param>
        /// <param name="propertyDefs">The property defs.</param>
        /// <param name="subClass">if set to <c>true</c> [sub class].</param>
        /// <exception cref="System.Exception"></exception>
        private static void GatherColumnAndPropertyDefs(DatabaseEntityClass databaseClass, List<ColumnDef> columnDefs, List<PropertyDef> propertyDefs, bool subClass,
            ref bool isObjectID, ref bool isObjectNo) {
            var baseDatabaseClass = databaseClass.BaseClass as DatabaseEntityClass;
            if (baseDatabaseClass != null) {
                GatherColumnAndPropertyDefs(baseDatabaseClass, columnDefs, propertyDefs, true, ref isObjectID, ref isObjectNo);
            }

            var databaseAttributes = databaseClass.Attributes;
            for (int i = 0; i < databaseAttributes.Count; i++) {
                var databaseAttribute = databaseAttributes[i];

                DbTypeCode type;
                string targetTypeName = null;

                var databaseAttributeType = databaseAttribute.AttributeType;

                DatabasePrimitiveType databasePrimitiveType;
                DatabaseEnumType databaseEnumType;
                DatabaseEntityClass databaseEntityClass;
                DatabaseArrayType databaseArrayType;

                try {
                    if ((databasePrimitiveType = databaseAttributeType as DatabasePrimitiveType) != null) {
                        type = PrimitiveToTypeCode(databasePrimitiveType.Primitive);
                    } else if ((databaseEnumType = databaseAttributeType as DatabaseEnumType) != null) {
                        type = PrimitiveToTypeCode(databaseEnumType.UnderlyingType);
                    } else if ((databaseEntityClass = databaseAttributeType as DatabaseEntityClass) != null) {
                        type = DbTypeCode.Object;
                        targetTypeName = databaseEntityClass.Name;
                    } else if ((databaseArrayType = databaseAttributeType as DatabaseArrayType) != null) {
                        type = DbTypeCode.String;
                    } else {
                        if (!databaseAttribute.IsPersistent) continue;

                        // This type is not supported (but theres no way code will
                        // ever reach here unless theres some internal error). We
                        // just  raise an internal exception indicating the field
                        // and that this condition was experienced (indicating an
                        // internal bug).
                        // 
                        // In comment to the above: appearently, the code in the
                        // weaver is not up-to-date with the latest changes done in
                        // the code host. So let's provide an informative exception
                        // here anyway, helping our users to help themselves.
                        throw new NotSupportedException();
                    }
                } catch (NotSupportedException) {
                    throw ErrorCode.ToException(
                        Error.SCERRUNSUPPORTEDATTRIBUTETYPE,
                        string.Format(
                        "The attribute type of attribute {0}.{1} was found invalid.",
                        databaseAttribute.DeclaringClass.Name,
                        databaseAttribute.Name
                        ));
                }

                var isNullable = databaseAttribute.IsNullable;
                var isSynonym = databaseAttribute.SynonymousTo != null;

                // Fix handling that always nullable types are correcly
                // tagged as nullable in the schema file.

                switch (type) {
                    case DbTypeCode.Object:
                    case DbTypeCode.String:
                    case DbTypeCode.Binary:
                        isNullable = true;
                        break;
                }

                switch (databaseAttribute.AttributeKind) {
                    case DatabaseAttributeKind.Field:
                        if (!isSynonym) {
                            columnDefs.Add(new ColumnDef(
                                DotNetBindingHelpers.CSharp.BackingFieldNameToPropertyName(databaseAttribute.Name),
                                BindingHelper.ConvertDbTypeCodeToScTypeCode(type),
                                isNullable,
                                subClass
                                ));
                        }

                        var targetAttribute = databaseAttribute.SynonymousTo ?? databaseAttribute;

                        if (databaseAttribute.IsPublicRead) {
                            var propertyDef = new PropertyDef(
                                 databaseAttribute.Name,
                                 type,
                                 isNullable,
                                 targetTypeName
                                 );
                            propertyDef.ColumnName = targetAttribute.Name;
                            propertyDef.SpecialFlags = databaseAttribute.SpecialFlags;
                            AddProperty(propertyDef, propertyDefs);
                        }
                        break;
                    case DatabaseAttributeKind.Property:
                        if (databaseAttribute.IsPublicRead) {
                            var propertyDef = new PropertyDef(
                                databaseAttribute.Name,
                                type,
                                isNullable,
                                targetTypeName
                                );

                            string columnName = null;
                            var backingField = databaseAttribute.BackingField;
                            if (backingField != null && backingField.AttributeKind == DatabaseAttributeKind.Field) {
                                columnName = DotNetBindingHelpers.CSharp.BackingFieldNameToPropertyName(backingField.Name);
                            }
                            propertyDef.SpecialFlags = databaseAttribute.SpecialFlags;
                            propertyDef.ColumnName = columnName;
                            AddProperty(propertyDef, propertyDefs);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="property"></param>
        /// <param name="properties"></param>
        private static void AddProperty(PropertyDef property, List<PropertyDef> properties) {
            int index = properties.FindIndex( (match) => {
                if (property.Name.Equals(match.Name))
                    return true;
                return false;
            });

            if (index == -1) {
                // New property, just add it last.
                properties.Add(property);
            } else {
                // Property exist in baseclass. 
                properties[index] = property;
            }
        }

        /// <summary>
        /// Primitives to type code.
        /// </summary>
        /// <param name="primitive">The primitive.</param>
        /// <returns>DbTypeCode.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
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
            case DatabasePrimitive.None:
            default:
                throw new NotSupportedException();
            }
        }
    }
}
