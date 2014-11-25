
namespace Sc.Server.Weaver {
    /// <summary>
    /// Defines a set of names used by the weaver for internal
    /// and/or implicit constructs. These names are normally
    /// reserved, and will be guarded against collision against
    /// during weaving.
    /// </summary>
    public static class WeavedNames {
        /// <summary>
        /// Gets the full name of the implicit entity class.
        /// </summary>
        public const string ImplicitEntityClass = "Starcounter.ImplicitEntity";

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
    }
}