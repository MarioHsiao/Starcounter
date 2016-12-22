
using Starcounter.Hosting;
using System.Linq;
using System.Reflection;

namespace Starcounter.Weaver.Diagnostics
{
    /// <summary>
    /// Weaver-related assembly extension methods.
    /// </summary>
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Return a result indicating if a given assembly is weaved.
        /// </summary>
        /// <param name="assembly">The candidate assembly</param>
        /// <returns>True if the assembly is weaved; false otherwise.</returns>
        public static bool IsWeaved(this Assembly assembly)
        {
            return assembly.DefinedTypes.Any((type) => {
                return type.Name.Equals(AssemblySpecification.Name);
            });
        }
    }
}