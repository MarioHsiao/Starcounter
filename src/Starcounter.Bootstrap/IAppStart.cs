
using Starcounter.Bootstrap.Hosting;

namespace Starcounter.Bootstrap
{
    /// <summary>
    /// Defines the traits of an app that is to be started.
    /// </summary>
    /// <remarks>
    /// The normal use of this is when there is a desire to bootstrap
    /// and application as part bootstrapping the runtime host, e.g
    /// the "auto start" feature we support from the past.
    /// </remarks>
    public interface IAppStart
    {
        /// <summary>
        /// The full path to the assembly that is to be loaded by the host.
        /// </summary>
        string AssemblyPath { get; }

        /// <summary>
        /// The logical application working directory.
        /// </summary>
        string WorkingDirectory { get; }

        /// <summary>
        /// Options that dictate how the host should consider an application
        /// entrypoint ("Main").
        /// </summary>
        EntrypointOptions EntrypointOptions { get; }

        /// <summary>
        /// Arguments the host should pass to an entrypoint, if found and
        /// instucted to run.
        /// </summary>
        string[] EntrypointArguments { get; }
    }
}