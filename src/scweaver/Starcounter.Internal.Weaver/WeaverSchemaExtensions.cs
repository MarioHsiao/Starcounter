// ***********************************************************************
// <copyright file="WeaverSchemaExtensions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using Sc.Server.Weaver.Schema;

namespace Starcounter.Internal.Weaver {
    using System.Collections.Generic;
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

    /// <summary>
    /// Class WeaverSchemaExtensions
    /// </summary>
    internal static class WeaverSchemaExtensions {
        /// <summary>
        /// Tags the assmbly as one loaded from the weaver cache.
        /// </summary>
        /// <param name="assembly">The assembly to tag as being loaded from
        /// the weaver cache.</param>
        public static void SetIsLoadedFromCache(this DatabaseAssembly assembly) {
            assembly.Tags["IsLoadedFromCache"] = bool.TrueString;
        }

        /// <summary>
        /// Gets a value indicating if the given assembly is loaded from the
        /// weaver cache.
        /// </summary>
        /// <param name="assembly">The assembly to consult.</param>
        /// <returns><c>true</c> if <paramref name="assembly"/> was loaded
        /// from the weaver cache; <c>false</c> otherwise</returns>
        public static bool GetIsLoadedFromCache(this DatabaseAssembly assembly) {
            return assembly.Tags.ContainsKey("IsLoadedFromCache");
        }

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
            return ReadTagFromElement<TypeDefDeclaration>(databaseClass, "TypeDef");
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
            return ReadTagFromElement<FieldDefDeclaration>(databaseAttribute, "FieldDef");
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
            return ReadTagFromElement<PropertyDeclaration>(databaseAttribute, "PropertyDef");
        }

        static T ReadTagFromElement<T>(DatabaseSchemaElement e, string tag) {
            try {
                return (T) e.Tags[tag];
            } catch (KeyNotFoundException notFound) {
                var databaseClass = e as DatabaseClass;
                if (databaseClass == null) {
                    var a = e as DatabaseAttribute;
                    if (a != null) databaseClass = a.DeclaringClass;
                }
                if (databaseClass == null)
                    throw;
                if (!databaseClass.Assembly.GetIsLoadedFromCache())
                    throw;

                // The problem is that we have tried reading a tag from an
                // assembly that was cached, something we currently don't support,
                // and we report this as an internal error.

                throw ErrorCode.ToException(Error.SCERRWEAVERCANTUSECACHE, notFound, string.Format("Assembly: {0}", databaseClass.Assembly));
            }
        }
    }
}