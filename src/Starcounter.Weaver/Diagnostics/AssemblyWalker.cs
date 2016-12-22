
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Starcounter.Weaver.Diagnostics
{
    /// <summary>
    /// Allow walking of candidate assembly files in a file system.
    /// </summary>
    public class AssemblyWalker
    {
        readonly string dir;
        readonly Func<string, Assembly> loader;

        /// <summary>
        /// Initialize a new <see cref="AssemblyWalker"/>.
        /// </summary>
        /// <param name="directory">Directory where files are.</param>
        /// <param name="assemblyLoader">Assembly loader authority.</param>
        public AssemblyWalker(string directory, Func<string, Assembly> assemblyLoader)
        {
            Guard.DirectoryExists(directory, nameof(directory));
            Guard.NotNull(assemblyLoader, nameof(assemblyLoader));

            dir = directory;
            loader = assemblyLoader;
        }

        /// <summary>
        /// Iterate all files via the loader.
        /// </summary>
        /// <returns>List of materialized assemblies, governed by the loader.</returns>
        public IEnumerable<Assembly> Walk()
        {
            var filter = new [] { ".exe", ".dll" };

            foreach (var item in Directory.EnumerateFiles(dir, "*.*").Where(s => filter.Any(ext => ext == Path.GetExtension(s))))
            {
                var assembly = loader(item);
                if (assembly != null)
                {
                    yield return assembly;
                }
            }
        }
    }
}
