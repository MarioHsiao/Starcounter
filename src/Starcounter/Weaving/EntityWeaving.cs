using Sc.Server.Weaver.Schema;
using System;

namespace Sc.Server.Weaver {
    /// <summary>
    /// Utility class used by the weaver and (implicitly) the
    /// code host loader to correctly weave the <see cref="Entity"/>
    /// class.
    /// </summary>
    public static class EntityWeaving {
        /// <summary>
        /// Defines a weaver schema database class representing the
        /// Starcounter <see cref="Entity"/> class.
        /// </summary>
        /// <param name="assembly">The assembly to define the class in.
        /// </param>
        /// <returns>A weaver schema database class.</returns>
        public static DatabaseEntityClass DefineEntityClass(DatabaseAssembly assembly) {
            var entity = new DatabaseEntityClass(assembly, typeof(Starcounter.Entity).FullName);
            DefineImplicitFields(entity, entity);

            AddDefinedProperty(entity, "Type", entity, entity.Attributes[WeavedNames.TypeColumn]);
            AddDefinedProperty(entity, "TypeInherits", entity, entity.Attributes[WeavedNames.InheritsColumn]);
            AddDefinedProperty(
                entity,
                "Name", 
                DatabasePrimitiveType.GetInstance(DatabasePrimitive.String),
                entity.Attributes[WeavedNames.TypeNameColumn]
                );

            AddDefinedProperty(
                entity, 
                "IsType",
                DatabasePrimitiveType.GetInstance(DatabasePrimitive.Boolean),
                entity.Attributes[WeavedNames.IsTypeColumn]
                );

            return entity;
        }

        /// <summary>
        /// Defines a weaver schema database class representing the
        /// Starcounter implicit Entity class, used to specify an implicit
        /// base type database classes not deriving <see cref="Entity"/>
        /// will be specializing.
        /// </summary>
        /// <param name="assembly">The assembly to define the class in.
        /// </param>
        /// <returns>A weaver schema database class.</returns>
        public static DatabaseEntityClass DefineImplicitEntityClass(DatabaseAssembly assembly) {
            var entity = assembly.Schema.FindDatabaseClass(typeof(Starcounter.Entity).FullName);

            var implicitEntity = new DatabaseEntityClass(assembly, WeavedNames.ImplicitEntityClass);
            DefineImplicitFields(implicitEntity, entity as DatabaseEntityClass);
            return implicitEntity;
        }

        static void DefineImplicitFields(DatabaseEntityClass classBuilder, DatabaseEntityClass entityClass) {
            var field = AddDefinedField(classBuilder, WeavedNames.TypeColumn, entityClass);
            field.IsTypeReference = true;
            field.IsNullable = true;

            field = AddDefinedField(classBuilder, WeavedNames.InheritsColumn, entityClass);
            field.IsInheritsReference = true;
            field.IsNullable = true;

            field = AddDefinedField(classBuilder, WeavedNames.TypeNameColumn, DatabasePrimitiveType.GetInstance(DatabasePrimitive.String));
            field.IsTypeName = true;
            field.IsNullable = true;

            field = AddDefinedField(classBuilder, WeavedNames.IsTypeColumn, DatabasePrimitiveType.GetInstance(DatabasePrimitive.Boolean));
            field.IsNullable = false;

        }

        static DatabaseAttribute AddDefinedField(DatabaseEntityClass classBuilder, string name, IDatabaseAttributeType type) {
            var databaseAttribute = new DatabaseAttribute(classBuilder, name);
            classBuilder.Attributes.Add(databaseAttribute);
            databaseAttribute.IsInitOnly = false;
            databaseAttribute.AttributeKind = DatabaseAttributeKind.Field;
            databaseAttribute.AttributeType = type;
            return databaseAttribute;
        }

        static DatabaseAttribute AddDefinedProperty(
            DatabaseEntityClass classBuilder, 
            string name,
            IDatabaseAttributeType type,
            DatabaseAttribute backingField) {
            var databaseAttribute = new DatabaseAttribute(classBuilder, name);
            classBuilder.Attributes.Add(databaseAttribute);
            databaseAttribute.IsInitOnly = false;
            databaseAttribute.AttributeKind = DatabaseAttributeKind.Property;
            databaseAttribute.BackingField = backingField;
            databaseAttribute.AttributeType = type;
            return databaseAttribute;
        }
    }
}