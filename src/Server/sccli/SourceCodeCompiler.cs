using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// Compiles a source code file.
        /// </summary>
        /// <param name="sourceCode">The source code to compile.</param>
        /// <param name="assemblyPath">Path to the compiled assembly.</param>
        public static void CompileSingleFileToExecutable(
            string sourceCode, out string assemblyPath) {
            throw new NotImplementedException();
        }
    }
}
