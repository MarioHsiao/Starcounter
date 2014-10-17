
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Extensibility;
using Starcounter;
using System;

namespace Starcounter.Weaver {
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

        /// <inheritdoc/>
        public override ModuleDeclaration Load(Domain domain) {
            try {
                return base.Load(domain);
            } catch (Exception e) {
                var postfix = string.Format("File: {0}", this.FileName);
                throw ErrorCode.ToException(Error.SCERRWEAVERFAILEDLOADFILE, e, postfix);
            }
        }
    }
}
