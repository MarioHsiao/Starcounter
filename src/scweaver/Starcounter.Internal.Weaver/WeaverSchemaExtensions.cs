// ***********************************************************************
// <copyright file="WeaverSchemaExtensions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using Sc.Server.Weaver.Schema;

namespace Starcounter.Internal.Weaver {
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

    /// <summary>
    /// Class WeaverSchemaExtensions
    /// </summary>
    internal static class WeaverSchemaExtensions {
        /// <summary>
        /// Sets the type definition.
        /// </summary>
        /// <param name="databaseClass">The database class.</param>
        /// <param name="typeDef">The type def.</param>
        public static void SetTypeDefinition(this DatabaseClass databaseClass, TypeDefDeclaration typeDef) {
            databaseClass.Tags["TypeDef"] = typeDef;
        }

        /// <summary>
        /// Gets the type definition.
        /// </summary>
        /// <param name="databaseClass">The database class.</param>
        /// <returns>TypeDefDeclaration.</returns>
        public static TypeDefDeclaration GetTypeDefinition(this DatabaseClass databaseClass) {
            return (TypeDefDeclaration)databaseClass.Tags["TypeDef"];
        }

        /// <summary>
        /// Sets the field definition.
        /// </summary>
        /// <param name="databaseAttribute">The database attribute.</param>
        /// <param name="fieldDef">The field def.</param>
        public static void SetFieldDefinition(this DatabaseAttribute databaseAttribute, FieldDefDeclaration fieldDef) {
            databaseAttribute.Tags["FieldDef"] = fieldDef;
        }

        /// <summary>
        /// Gets the field definition.
        /// </summary>
        /// <param name="databaseAttribute">The database attribute.</param>
        /// <returns>FieldDefDeclaration.</returns>
        public static FieldDefDeclaration GetFieldDefinition(this DatabaseAttribute databaseAttribute) {
            return (FieldDefDeclaration)databaseAttribute.Tags["FieldDef"];
        }

        /// <summary>
        /// Sets the property definition.
        /// </summary>
        /// <param name="databaseAttribute">The database attribute.</param>
        /// <param name="property">The property.</param>
        public static void SetPropertyDefinition(this DatabaseAttribute databaseAttribute, PropertyDeclaration property) {
            databaseAttribute.Tags["PropertyDef"] = property;
        }

        /// <summary>
        /// Gets the property definition.
        /// </summary>
        /// <param name="databaseAttribute">The database attribute.</param>
        /// <returns>PropertyDeclaration.</returns>
        public static PropertyDeclaration GetPropertyDefinition(this DatabaseAttribute databaseAttribute) {
            return (PropertyDeclaration)databaseAttribute.Tags["PropertyDef"];
        }
    }
}