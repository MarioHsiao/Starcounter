
using System;

namespace Starcounter.Hosting {
    /// <summary>
    /// Defines the interface of a host governing the runtime initialization
    /// of the backing infrastructure.
    /// </summary>
    /// <remarks>
    /// The primary use of an interface is that it allows us to create test
    /// doubles to test backing with fake data.
    /// </remarks>
    public interface IBackingHost {
        /// <summary>
        /// Initialize the backing infrastructure type specification
        /// represented it's <see cref="Type"/>.
        /// </summary>
        /// <param name="typeSpec">The <see cref="Type"/> of the type
        /// specification to initialize.</param>
        void InitializeTypeSpecification(Type typeSpec);
    }
}