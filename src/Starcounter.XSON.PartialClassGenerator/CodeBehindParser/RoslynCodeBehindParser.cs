using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Starcounter.XSON.Metadata;
using System.IO;

namespace Starcounter.XSON.PartialClassGenerator {

    /// <summary>
    /// Generates code behind metadata using Microsoft.CodeAnalysis.CSharp
    /// to parse and analyze the code behind content.
    /// </summary>
    internal class RoslynCodeBehindParser {
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        public readonly string ClassName;
        /// <summary>
        /// Gets the full source code (normally read from a file).
        /// </summary>
        public readonly string SourceCode;
        /// <summary>
        /// Gets the file path of the source code file, if present.
        /// </summary>
        public readonly string FilePath;

        public string FileNameNotNull {
            get {
                var path = FilePath;
                return path != null ? Path.GetFileName(path) : "<codebehind.json>";
            }
        }

        /// <summary>
        /// Initialize a new <see cref="RoslynCodeBehindParser"/>.
        /// </summary>
        /// <param name="className">Name of the class</param>
        /// <param name="sourceCode">Source code to parse</param>
        /// <param name="filePath">Full path to the source code file</param>
        public RoslynCodeBehindParser(string className, string sourceCode, string filePath) {
            this.ClassName = className;
            this.SourceCode = sourceCode;
            this.FilePath = filePath;
        }

        /// <summary>
        /// Generates code behind metadata by analzying the source code.
        /// </summary>
        /// <returns></returns>
        public CodeBehindMetadata ParseToMetadata() {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceCode);
            var root = (CompilationUnitSyntax)tree.GetRoot();
            var walker = new CodeBehindFileAnalyzer(this);
            walker.Visit(root);
            return walker.Result;
        }
    }
}
