
using System;
using System.Reflection;

namespace Starcounter.Hosting
{
    /// <summary>
    /// Defines the behavior of a custom assembly resolver.
    /// </summary>
    public interface IAssemblyResolver
    {
        /// <summary>
        /// Register the given application executable with the resolver.
        /// </summary>
        /// <param name="executablePath">Path to the executable file.</param>
        /// <returns>A virtual application directory that the host can use to resolve
        /// assemblies and schema files from.</returns>
        ApplicationDirectory RegisterApplication(string executablePath);

        /// <summary>
        /// Resolves the application main assembly given a path.
        /// </summary>
        /// <param name="executablePath">The path to the main assembly.</param>
        /// <returns>The main assembly loaded in the current domain.</returns>
        Assembly ResolveApplication(string executablePath);

        /// <summary>
        /// Resolve a reference when it can't be done by the CLR.
        /// </summary>
        /// <param name="args">Resolve arguments</param>
        /// <returns>A loaded assembly, or null it it could not be resolved.
        /// </returns>
        Assembly ResolveApplicationReference(ResolveEventArgs args);
    }
}