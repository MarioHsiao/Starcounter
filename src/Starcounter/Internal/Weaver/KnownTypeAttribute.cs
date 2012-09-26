
using System;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    
    // Strategy for known types:
    //
    // The weaver finds them by scanning the Starcounter assembly for
    // classes deriving from Entity (including Entity) and thoose
    // classes gets special treatment.
    //
    // Special treatmeant in this respect means:
    //
    //      Normal rules dont apply. Dont check constraints.
    //      Dont do any kind of transforming; just make them part of the
    //      metadata model without weaving.
    //      Properties should be able to be "fields". Not allowed in normal
    //      classes. Here, it should be allowed (and it should be verified
    //      that such a property only contains get/set and nothing more)
    //      Introduce possibility to force and/or suggest index for a field
    //      For the entity class, it must be forced.

    [Flags]
    public enum WeaverDirectives {
        None = 0,
        ExcludeConstraintValidation = 0x01,
        ExcludeAnyTransformation = 0x02,
        ExcludeSchemaDiscovery = 0x04,
        ExcludeConstructorTransformation = 0x08,
        AllowForbiddenDeclarations = 0x10,
        ExcludeAll = 0xFF
    }

    /// <summary>
    /// Custom attribute that, when applied on a type, means that this type requires a
    /// special processing from the analyzer or the weaver. Known types may be
    /// only present in known assemblies (see <see cref="KnownAssemblyAttribute"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class KnownTypeAttribute : Attribute {
        private readonly WeaverDirectives directives;

        /// <summary>
        /// Initializes a new <see cref="KnownTypeAttribute"/>.
        /// </summary>
        /// <param name="weaverDirectives">Weaver directives for the type to which the
        /// custom attribute is applied.</param>
        public KnownTypeAttribute(WeaverDirectives weaverDirectives) {
            this.directives = weaverDirectives;
        }

        /// <summary>
        /// Gets the weaver directives for the type to which the
        /// custom attribute is applied.
        /// </summary>
        public WeaverDirectives WeaverDirectives {
            get {
                return this.directives;
            }
        }
    }
}