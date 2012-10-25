// ***********************************************************************
// <copyright file="WeaverSchemaExtensions.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using Sc.Server.Weaver.Schema;
using Starcounter;
using System.Reflection;

namespace Starcounter.Internal.Weaver {
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

        /// <summary>
        /// Creates a representation of the built-in Starcounter assembly and stores it
        /// in the schema.
        /// </summary>
        /// <param name="schema">The schema.</param>
        public static void AddStarcounterAssembly(this DatabaseSchema schema) {
            DatabaseAssembly databaseAssembly;

            databaseAssembly = new DatabaseAssembly("Starcounter", Assembly.GetExecutingAssembly().FullName);
            databaseAssembly.IsCached = true;
            databaseAssembly.SetSchema(schema);
            schema.Assemblies.Add(databaseAssembly);
            databaseAssembly.DatabaseClasses.Add(new DatabaseEntityClass(databaseAssembly, typeof(Entity).FullName));

            // The built-in types, and the metadata "Table" class - what should
            // we do with them in 2.2?
            // TODO:

            //DatabaseEntityClass entityClass;
            //DatabaseEntityClass builtInType;
            //builtInType = new DatabaseEntityClass(databaseAssembly, typeof(Entity).FullName);
            //databaseAssembly.DatabaseClasses.Add(builtInType);
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "__InstantiatedFrom")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = builtInType,
            //    PersistentProperty = new DatabasePersistentProperty
            //    {
            //        AttributeFieldIndex = "_aiInstantiatedFrom"
            //    }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "__Instantiates")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = builtInType,
            //    PersistentProperty = new DatabasePersistentProperty
            //    {
            //        AttributeFieldIndex = "_aiInstantiates"
            //    }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "__DerivedFrom")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = builtInType,
            //    PersistentProperty = new DatabasePersistentProperty
            //    {
            //        AttributeFieldIndex = "_aiDerivedFrom"
            //    }
            //});
            //entityClass = builtInType;

            //// Create a representation for the UserDomain class

            //builtInType = new DatabaseEntityClass(databaseAssembly, typeof(UserDomain).FullName)
            //{
            //    BaseClass = entityClass
            //};
            //databaseAssembly.DatabaseClasses.Add(builtInType);
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "Name")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.String),
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiName" }
            //});

            //// Create a representation for the StarcounterUser class

            //builtInType = new DatabaseEntityClass(databaseAssembly, typeof(StarcounterUser).FullName)
            //{
            //    BaseClass = entityClass
            //};
            //databaseAssembly.DatabaseClasses.Add(builtInType);
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "Culture")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.String),
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiCulture" }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "Domain")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = builtInType,
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiDomain" }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "IsEnabled")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.Boolean),
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiIsEnabled" }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "Name")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.String),
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiName" }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "Password")
            //{
            //    AttributeKind = DatabaseAttributeKind.PersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.String),
            //    PersistentProperty = new DatabasePersistentProperty { AttributeFieldIndex = "_aiPassword" }
            //});
            //builtInType.Attributes.Add(new DatabaseAttribute(builtInType, "HasPassword")
            //{
            //    AttributeKind = DatabaseAttributeKind.NotPersistentProperty,
            //    AttributeType = DatabasePrimitiveType.GetInstance(DatabasePrimitive.Boolean),
            //});

            //// Add the SysTable class.

            //databaseAssembly.DatabaseClasses.Add(
            //    SysTable.GetDatabaseClass(databaseAssembly, entityClass)
            //    );
        }
    }
}