
using System.Collections.Generic;

namespace Starcounter.UnitTesting
{
    /// <summary>
    /// Define the semantics of a root node in a tree of tests.
    /// </summary>
    public interface ITestRoot
    {
        /// <summary>
        /// Get all hosts under the current root.
        /// </summary>
        IEnumerable<TestHost> Hosts { get; }

        /// <summary>
        /// Include a new host for the specified application.
        /// </summary>
        /// <param name="application">Name of application.
        /// </param>
        /// <param name="factory">Optional factory to use,
        /// overriding any default.</param>
        /// <returns></returns>
        TestHost IncludeNewHost(string application, ITestHostFactory factory = null);
    }
}