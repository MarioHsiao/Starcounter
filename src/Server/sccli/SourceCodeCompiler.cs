using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;

namespace star {
    /// <summary>
    /// Implements the source code compiling capabilities of the star.exe
    /// CLI tool.
    /// </summary>
    /// <remarks>
    ///  No IoC yet, but kind of the opposite - there is a very tight two-
    ///  way relation between the compiler and the program. This is by
    ///  design, since we still are just having this as kind of a aptit-
    ///  retare for what is to come (and hence should not spend time on
    ///  this just yet).
    ///  <para>
    ///  Probably, we will host this code somewhere else down the road;
    ///  we might want to allow stored procedure like programming over
    ///  the web for example.
    ///  </para>
    /// </remarks>
    class SourceCodeCompiler {
        /// <summary>
        /// Compiles a source code file to an executable. If the operation
        /// succeed, the path to the compiled assembly is returned.
        /// </summary>
        /// <param name="sourceCode">The source code to compile.</param>
        /// <param name="assemblyPath">Path to the compiled assembly.</param>
        public static void CompileSingleFileToExecutable(string sourceCode, out string assemblyPath) {
            var provider = CSharpCodeProvider.CreateProvider("CSharp");
            var parameters = new CompilerParameters() {
                GenerateExecutable = true,
                GenerateInMemory = false
            };
            parameters.TempFiles = new TempFileCollection(@".\.temp", true);
            parameters.OutputAssembly = Path.GetFullPath(string.Format(@".\.out\{0}.exe", Path.GetFileNameWithoutExtension(sourceCode)));
            parameters.ReferencedAssemblies.Add("Starcounter");

            // Specify a x64 bit application? Can 32-bit applications reference
            // Starcounter?
            // TODO:

            var result = provider.CompileAssemblyFromFile(parameters, sourceCode);
            if (result.Errors.Count > 0) {
                assemblyPath = null;
                foreach (var error in result.Errors) {
                    Console.WriteLine(error.ToString());
                }
                throw new Exception("Errors compiling!");
            }
            assemblyPath = result.PathToAssembly;

            Console.WriteLine("Compiled in {0}", assemblyPath);
            Environment.Exit(0);
        }
    }
}
