
using Starcounter.Ioc;
using System;

namespace Starcounter {
    /// <summary>
    /// Represents the running code host.
    /// </summary>
    public interface ICodeHost {
        /// <summary>
        /// Gets the services installed in the current code host;
        /// </summary>
        IServices Services { get; }

        /// <summary>
        /// Execute the code host, redirecting it to the given
        /// entrypoint.
        /// </summary>
        /// <param name="entrypoint">Entrypoint callback.</param>
        void Run(Action entrypoint);
    }
}