
using System;
using Starcounter.Binding;

namespace Starcounter.Internal.Metadata {

    /// <summary>
    /// Defines the semantics we need to bind metadata classes, and
    /// provides a way to inject a concrete binder from generated
    /// code.
    /// </summary>
    public abstract partial class MetaBinder {
        /// <summary>
        /// Gets the runtime instance of the binder. Assigned by
        /// generated code.
        /// </summary>
        public static MetaBinder Instance;

        /// <summary>
        /// Return the set of <see cref="TypeDef"/> instances representing
        /// metadata host classes.
        /// </summary>
        /// <returns>Metadata host definitions.</returns>
        public abstract TypeDef[] GetDefinitions();

        /// <summary>
        /// Gets the type of every type specification belonging to a
        /// metadata class.
        /// </summary>
        /// <returns>Types of specifications.</returns>
        public abstract Type[] GetSpecifications();
    }
}