
using System;
using Starcounter.Binding;

namespace Starcounter.Internal.Metadata {

    /// <summary>
    /// Defines the semantics we need to bind metadata classes, and
    /// provides a way to inject a concrete binder from generated
    /// code.
    /// </summary>
    public abstract partial class MetaBinder {
        public static MetaBinder Instance;

        public abstract TypeDef[] GetDefinitions();
        public abstract Type[] GetSpecifications();
    }
}