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
using Starcounter.Logging;

namespace Starcounter.Internal
{
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;
    
    /// <summary>
    /// Class LoaderHelper
    /// </summary>
    public static class LoaderHelper
    {
        // To be deprecated.
        // This functionality should be supported by the TypeDef
        // class instead; it's the responsibility of that type to
        // synchronize the host-level support arrays whenever the
        // underlying table layout it reference changed (like when
        // column indexing change because a table upgrade).




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
                    columnRuntimeTypes[ci] = BindingHelper.ConvertScTypeCodeToDbTypeCode(columnDefs[ci].Type);
                }
            }
        }
    }

    /// <summary>
    /// Class SchemaLoader
    /// </summary>
    public static class SchemaLoader
    {
        static int errorsFoundWithCodeScErrNonPublicFieldNotExposed;

        internal static LogSource Log = LogSources.CodeHostLoader;

        /// <summary>
        /// Loads the and convert schema.
        /// </summary>
        /// <param name="inputDir">The input dir.</param>
        /// <param name="types">List of all type definitions in the result
        /// that are determined to be custom dynamic type references.</param>
        /// <returns>List{TypeDef}.</returns>
        public static List<TypeDef> LoadAndConvertSchema(DirectoryInfo inputDir, out List<TypeDef> types)
        {
            errorsFoundWithCodeScErrNonPublicFieldNotExposed = 0;

            var typeDefs = new List<TypeDef>();
            var databaseSchema = DatabaseSchema.DeserializeFrom(inputDir);

            var databaseClasses = new List<DatabaseEntityClass>();
            databaseSchema.PopulateOrderedDatabaseEntityClasses2(databaseClasses);
            
            var databaseClassCount = databaseClasses.Count;
            for (int i = 0; i < databaseClassCount; i++)
            {
                var databaseClass = databaseClasses[i];

                var databaseAssembly = databaseClass.Assembly;
                var assemblyName = new AssemblyName(databaseAssembly.FullName);
                var typeLoader = new TypeLoader(assemblyName, databaseClass.Name);
                typeDefs.Add(EntityClassToTypeDef(databaseClass, typeLoader));
            }

            if (errorsFoundWithCodeScErrNonPublicFieldNotExposed > 0) {
                throw ErrorCode.ToException(Error.SCERRCANTBINDAPPWITHPRIVATEDATA,
                    string.Format("{0} illegal fields exist.", errorsFoundWithCodeScErrNonPublicFieldNotExposed));
            }

            var customTypes = databaseSchema.GetCompileTimeTypes();
            types = new List<TypeDef>(customTypes.Count);
            foreach (var customType in customTypes) {
                var td = typeDefs.Find(t => t.Name.Equals(customType.Name));
                types.Add(td);
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
            var hostedColumns = new List<HostedColumn>();

            string baseName = databaseClass.BaseClass == null ? null : databaseClass.BaseClass.Name;

            // Add column definition for implicit key column.
            var column = new ColumnDef("__id", sccoredb.STAR_TYPE_KEY, false, baseName == null ? false : true);
            columnDefs.Add(column);
            hostedColumns.Add(new HostedColumn() { Name = column.Name, TypeCode = DbTypeCode.Key });

            GatherColumnAndPropertyDefs(databaseClass, columnDefs, propertyDefs, hostedColumns, false);
            var columnDefArray = columnDefs.ToArray();
            var propertyDefArray = propertyDefs.ToArray();

            DbTypeCode[] columnRuntimeTypes;
            LoaderHelper.MapPropertyDefsToColumnDefs(
                columnDefArray, propertyDefArray, out columnRuntimeTypes
                );
            
            var tableDef = new TableDef(databaseClass.Name, baseName, columnDefArray);
            var typeDef = TypeDef.DefineNew(
                databaseClass.Name, baseName, tableDef, typeLoader, propertyDefArray, hostedColumns.ToArray());

            return typeDef;
        }

        /// <summary>
        /// Gathers the column and property defs.
        /// </summary>
        /// <param name="databaseClass">The database class.</param>
        /// <param name="columnDefs">The column defs.</param>
        /// <param name="propertyDefs">The property defs.</param>
        /// <param name="hostedColumns">Set of <see cref="HostedColumn"/> instances
        /// corresponding to every identified column.</param>
        /// <param name="subClass">if set to <c>true</c> [sub class].</param>
        /// <exception cref="System.Exception"></exception>
        private static void GatherColumnAndPropertyDefs(
            DatabaseEntityClass databaseClass, 
            List<ColumnDef> columnDefs, 
            List<PropertyDef> propertyDefs,
            List<HostedColumn> hostedColumns,
            bool subClass) {
            var baseDatabaseClass = databaseClass.BaseClass as DatabaseEntityClass;
            if (baseDatabaseClass != null) {
                GatherColumnAndPropertyDefs(baseDatabaseClass, columnDefs, propertyDefs, hostedColumns, true);
            }

            var databaseAttributes = databaseClass.Attributes;
            for (int i = 0; i < databaseAttributes.Count; i++) {
                var databaseAttribute = databaseAttributes[i];

                string targetTypeName = null;
                bool isNullable = false;
                bool isSynonym = false;

                var typeResult = ConvertDatabaseAttributeToVariables(databaseAttribute, ref targetTypeName, ref isNullable, ref isSynonym);
                if (!typeResult.HasValue) {
                    continue;
                }

                DbTypeCode type = typeResult.Value;

                switch (databaseAttribute.AttributeKind) {
                    case DatabaseAttributeKind.Field:
                        if (!isSynonym && !databaseAttribute.IsAccessorOfImplicitEntityField) {
                            var column = new ColumnDef(
                                DotNetBindingHelpers.CSharp.BackingFieldNameToPropertyName(databaseAttribute.Name),
                                BindingHelper.ConvertDbTypeCodeToScTypeCode(type),
                                isNullable,
                                subClass
                                );
                            columnDefs.Add(column);
                            hostedColumns.Add(new HostedColumn() { 
                                Name = column.Name,
                                TypeCode = type,
                                TargetType = targetTypeName 
                            });
                        }

                        var targetAttribute = databaseAttribute.SynonymousTo ?? databaseAttribute.EntityFieldTarget;
                        if (targetAttribute == null) {
                            targetAttribute = databaseAttribute;
                        }

                        if (databaseAttribute.IsDeclaredPublic) {
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

        static DbTypeCode? ConvertDatabaseAttributeToVariables(
            DatabaseAttribute attribute,
            ref string targetTypeName,
            ref bool isNullable,
            ref bool isSynonym) {

            DbTypeCode type;
            var databaseAttributeType = attribute.AttributeType;

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
                    if (!attribute.IsPersistent) {
                        return null;
                    }

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
                    attribute.DeclaringClass.Name,
                    attribute.Name
                    ));
            }

            isNullable = attribute.IsNullable;
            isSynonym = attribute.SynonymousTo != null;

            // Fix handling that always nullable types are correcly
            // tagged as nullable in the schema file.

            switch (type) {
                case DbTypeCode.Object:
                case DbTypeCode.String:
                case DbTypeCode.Binary:
                    isNullable = true;
                    break;
            }

            return type;
        }

        static void DetectColumnsWithNoPublicMapping(
            DatabaseEntityClass databaseClass,
            List<ColumnDef> columns,
            List<PropertyDef> properties) {
            
            if (databaseClass != null) {
                throw new NotImplementedException("This feature is no longer in use in this Starcounter version");
            }
            
            var baseDatabaseClass = databaseClass.BaseClass as DatabaseEntityClass;
            if (baseDatabaseClass != null) {
                DetectColumnsWithNoPublicMapping(baseDatabaseClass, columns, properties);
            }

            foreach (var attribute in databaseClass.Attributes) {

                bool doesNotApply = attribute.AttributeKind != DatabaseAttributeKind.Field;
                doesNotApply = doesNotApply || attribute.IsDeclaredPublic;
                if (doesNotApply) continue;

                string targetTypeName = null;
                bool isNullable = false;
                bool isSynonym = false;

                var typeResult = ConvertDatabaseAttributeToVariables(attribute, ref targetTypeName, ref isNullable, ref isSynonym);
                if (!typeResult.HasValue || isSynonym) {
                    continue;
                }

                // Find any property that map to this attribute
                var columnName = DotNetBindingHelpers.CSharp.BackingFieldNameToPropertyName(attribute.Name);
                var mapping = properties.Find((candidate) => {
                    return candidate.ColumnName == columnName;
                });
                if (mapping != null) continue;

                HandleColumnWithNoPublicMapping(
                    databaseClass,
                    columns,
                    properties,
                    attribute,
                    typeResult.Value,
                    targetTypeName,
                    isNullable
                    );
            }
        }

        static void HandleColumnWithNoPublicMapping(
            DatabaseEntityClass databaseClass,
            List<ColumnDef> columns,
            List<PropertyDef> properties,
            DatabaseAttribute attribute,
            DbTypeCode type,
            string targetTypeName,
            bool isNullable) {

            // This is our current strategy. Will change in upcoming
            // version. See #2508.

            Log.LogError(ErrorCode.ToMessage(Error.SCERRUNSPECIFIED, 
                string.Format("{0}.{1} not exposed", attribute.DeclaringClass.Name, attribute.Name)
                ));
            errorsFoundWithCodeScErrNonPublicFieldNotExposed++;
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
