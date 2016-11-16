
using Starcounter.Server.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Starcounter.CLI
{
    /// <summary>
    /// Provide a simple API to compile a single source Starcounter
    /// application file with common defaults.
    /// </summary>
    public class SourceCodeCompiler {
        /// <summary>
        /// Compiles a source code file to an executable. If the operation
        /// succeed, the path to the compiled assembly is returned.
        /// </summary>
        /// <param name="sourceCode">The source code to compile.</param>
        /// <param name="assemblyPath">Path to the compiled assembly.</param>
        public static void CompileSingleFileToExecutable(string sourceCode, out string assemblyPath)
        {
            var name = Path.GetFileNameWithoutExtension(sourceCode);
            var compiler = new AppCompiler(name)
                .WithDefaultReferences()
                .WithStarcounterAssemblyInfo()
                .WithSourceCodeFile(sourceCode);

            assemblyPath = null;
            try
            {
                var result2 = compiler.Compile();
                assemblyPath = result2.ApplicationPath;
            }
            catch (AppCompilerException e)
            {
                if (e.HasCompilerErrors)
                {
                    WriteCompilerErrorsToConsole(e.CompilerErrors);
                }

                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, e, $"Unable to compile {name}.cs to an app: compilation failed");
            }
        }

        static void WriteCompilerErrorsToConsole(IEnumerable<IAppCompilerSourceError> errors)
        {
            var errorCount = errors.Count();

            Console.WriteLine("Compilation errors ({0}):", errorCount);
            foreach (var error in errors)
            {
                WriteCompilerErrorToConsole(error);
            }
        }

        static void WriteCompilerErrorToConsole(IAppCompilerSourceError error)
        {
            var s = new StringBuilder()
                .Append(Path.GetFileName(error.File))
                .AppendFormat("({0},{1}): ", error.Line, error.Column)
                .AppendFormat("{0} {1}: ", "error", error.Id)
                .Append(error.Description)
                .ToString();

            Console.WriteLine(s);
        }
    }
}
