using Starcounter.Ioc;
using System;

namespace Starcounter.Hosting {

    /// <summary>
    /// Provide hosting specific extensions to <see cref="ICodeHost"/>.
    /// </summary>
    public static class CodeHostExtensions {
        /// <summary>
        /// Retreives the <see cref="IServiceContainer"/> of the given host,
        /// allowing participants to register services.
        /// </summary>
        /// <param name="host">The code host whose container to retreive.</param>
        /// <returns>The service container.</returns>
        public static IServiceContainer GetServiceContainer(this ICodeHost host) {
            return DefaultHost.Current.ServiceContainer;
        }
    }
}