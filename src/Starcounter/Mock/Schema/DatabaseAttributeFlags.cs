
namespace Sc.Server.Weaver.Schema {
    /// <summary>
    /// Flags that influence the behaviour/semantics of the database
    /// attrbute they are applied to.
    /// </summary>
    public sealed class DatabaseAttributeFlags {
        /// <summary>
        /// Indicates the target database attribute is a type reference.
        /// </summary>
        public const int TypeReference = 1;
        /// <summary>
        /// Indicates the target database attribute is a inherits reference.
        /// </summary>
        public const int IneritsReference = 2;

        /// <summary>
        /// Indicates the target database attribute contains the type name
        /// of the declaring class.
        /// </summary>
        public const int TypeName = 4;
    }
}
