
using PostSharp.Sdk.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weaver {
    /// <summary>
    /// The strategy used by the code weaver to load assemblies.
    /// </summary>
    internal sealed class CodeWeaverModuleLoadStrategy : ModuleLoadDirectFromFileStrategy {
        /// <summary>
        /// Gets the file name of the assembly being loaded by this
        /// strategy.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Initializes a <see cref="CodeWeaverModuleLoadStrategy"/> with the
        /// given file.
        /// </summary>
        /// <param name="fileName">The file name (and, possibly, path) of the
        /// assembly to load.</param>
        public CodeWeaverModuleLoadStrategy(string fileName)
            : base(fileName, false) {
            this.FileName = fileName;
        }
    }
}
