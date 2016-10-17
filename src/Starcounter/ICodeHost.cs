
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
        /// Execute the code host, passing a pointer to the application
        /// main loop that are to be executed when the host implementation
        /// consider itself ready for service.
        /// </summary>
        /// <param name="applicationMainLoop">
        /// Application main lopp callback.
        /// </param>
        void Run(Action applicationMainLoop);
    }
}