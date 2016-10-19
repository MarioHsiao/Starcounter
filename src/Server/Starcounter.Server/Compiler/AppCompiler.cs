using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Compiler that allows programmatic compilation of Starcounter
    /// apps.
    /// </summary>
    public sealed class AppCompiler {
        readonly string tempPath;

        /// <summary>
        /// Gets or sets the name to use for the compiled application.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the target assembly path. If specified before
        /// the app is compiled, the compiler will emit the assembly to
        /// this path, and remove any temporary artifacts.
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Holds source files that should be included in the compilation.
        /// </summary>
        public List<string> SourceFiles = new List<string>();

        /// <summary>
        /// Holds a set of source code strings that should be included in
        /// the compilation.
        /// </summary>
        public List<string> SourceCode = new List<string>();

        /// <summary>
        /// Initialize a new <see cref="AppCompiler"/> instance.
        /// </summary>
        public AppCompiler(string applicationName) {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            Name = applicationName;

            var guid = Guid.NewGuid().ToString();
            var tp = Path.GetTempPath();
            tempPath = Path.Combine(tp, guid);
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
        }

        /// <summary>
        /// Compiles the included source code into an application.
        /// </summary>
        public AppCompilerResult Compile() {
            if (SourceCode.Count == 0 && SourceFiles.Count == 0) {
                throw new AppCompilerException(AppCompilerError.NoSourceSpecified);
            }

            var provider = CSharpCodeProvider.CreateProvider("CSharp");
            var parameters = new CompilerParameters() {
                GenerateExecutable = true,
                GenerateInMemory = false,
                IncludeDebugInformation = true
            };

            Directory.CreateDirectory(tempPath);

            var result = new AppCompilerResult(Name, tempPath);

            parameters.TempFiles = new TempFileCollection(result.OutputDirectory, false);
            parameters.TempFiles.AddFile(result.ApplicationPath, true);
            parameters.TempFiles.AddFile(result.SymbolFilePath, true);

            parameters.OutputAssembly = result.ApplicationPath;

            var compilerResult = provider.CompileAssemblyFromFile(parameters, SourceCode.ToArray());
            try
            {
                if (compilerResult.Errors.Count > 0)
                {
                    RaiseCompilationError(compilerResult);
                }
            }
            finally
            {
                SafeDeleteTempFiles(compilerResult);
            }

            return result;
        }

        void RaiseCompilationError(CompilerResults result)
        {
            // Handle compilation errors
            // TODO:
        }

        void SafeDeleteTempFiles(CompilerResults result)
        {
            if (result.TempFiles != null)
            {
                try
                {
                    result.TempFiles.Delete();
                }
                catch { }
            }
        }
    }
}
