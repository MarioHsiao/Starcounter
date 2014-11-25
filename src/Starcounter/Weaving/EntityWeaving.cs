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
            var typeRef = new DatabaseAttribute(entity, "Type");
            typeRef.BackingField = new DatabaseAttribute(entity, "__sc__type__");
            typeRef.AttributeKind = DatabaseAttributeKind.Property;
            typeRef.AttributeType = entity;
            entity.Attributes.Add(typeRef);
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
        public static DatabaseEntityClass DefineImpliciEntityClass(DatabaseAssembly assembly) {
            throw new NotImplementedException();
        }
    }
}
