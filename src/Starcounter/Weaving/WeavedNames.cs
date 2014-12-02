
using Starcounter;
using Starcounter.Internal;

namespace Sc.Server.Weaver {
    using DatabaseAttribute = Sc.Server.Weaver.Schema.DatabaseAttribute;

    /// <summary>
    /// Defines a set of names used by the weaver for internal
    /// and/or implicit constructs. These names are normally
    /// reserved, and will be guarded against collision against
    /// during weaving.
    /// </summary>
    public static class WeavedNames {
        /// <summary>
        /// Gets the full name of the Entity class.
        /// </summary>
        public static string EntityClass {
            get { return typeof(Entity).FullName; }
        }

        /// <summary>
        /// Gets the full name of the implicit entity class.
        /// </summary>
        public static string ImplicitEntityClass {
            get { return typeof(ImplicitEntity).FullName; }
        }

        /// <summary>
        /// Gets the name of the implicit type column.
        /// </summary>
        public const string TypeColumn = "__sc__type__";

        /// <summary>
        /// Gets the name of the implicit type name column
        /// </summary>
        public const string TypeNameColumn = "__sc__type_name__";

        /// <summary>
        /// Gets the name of the implicit inherits column.
        /// </summary>
        public const string InheritsColumn = "__sc__inherits__";

        /// <summary>
        /// Gets the name of the implicit column that indicates if
        /// an entity is a type.
        /// </summary>
        public const string IsTypeColumn = "__sc__is_type__";

        /// <summary>
        /// Gets the name of the implicit entity column the given
        /// <see cref="DatabaseAttribute"/> reference, if any; or
        /// <c>null</c> otherwise.
        /// </summary>
        /// <param name="attribute">The attribute to consult.</param>
        /// <returns>Name of the implicit entity column
        /// <paramref name="attribute"/> is an accessor for, or <c>null</c>
        /// if it doesn't specify such reference.
        /// </returns>
        public static string GetImplicitEntityColumnName(DatabaseAttribute attribute) {
            string result = null;
            if (attribute.IsTypeReference) {
                result = TypeColumn;
            } else if (attribute.IsInheritsReference) {
                result = InheritsColumn;
            } else if (attribute.IsTypeName) {
                result = TypeNameColumn;
            }
            return result;
        }
    }
}