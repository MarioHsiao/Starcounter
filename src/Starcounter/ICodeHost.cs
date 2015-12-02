
using Starcounter.Ioc;

namespace Starcounter {
    /// <summary>
    /// Represents the running code host.
    /// </summary>
    public interface ICodeHost {
        /// <summary>
        /// Gets the services installed in the current code host;
        /// </summary>
        IServices Services { get; }
    }
}