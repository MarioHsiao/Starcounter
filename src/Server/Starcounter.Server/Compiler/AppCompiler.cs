using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Compiler that allows programmatic compilation of Starcounter
    /// apps.
    /// </summary>
    public sealed class AppCompiler
    {
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
        /// List of references that will be used to compile the application.
        /// </summary>
        public Dictionary<string, bool> MetadataReferences = new Dictionary<string, bool>();

        /// <summary>
        /// Initialize a new <see cref="AppCompiler"/> instance.
        /// </summary>
        public AppCompiler(string applicationName)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName));
            }

            Name = applicationName;
        }
        
        /// <summary>
        /// Compiles the included source code into an application.
        /// </summary>
        public AppCompilerResult Compile()
        {
            if (SourceCode.Count == 0 && SourceFiles.Count == 0)
            {
                throw new AppCompilerException(AppCompilerError.NoSourceSpecified);
            }

            var targetPath = TargetPath ?? CreateTempDirectory();
            var result = new AppCompilerResult(Name, targetPath);

            var parameters = new CompilerParameters()
            {
                GenerateExecutable = true,
                GenerateInMemory = false,
                IncludeDebugInformation = true,
                OutputAssembly = result.ApplicationPath
            };

            parameters.TempFiles = new TempFileCollection(result.OutputDirectory, false);
            parameters.TempFiles.AddFile(result.ApplicationPath, true);
            parameters.TempFiles.AddFile(result.SymbolFilePath, true);

            var sources = new List<string>(SourceFiles);
            if (SourceCode.Count > 0)
            {
                var tempSources = CreateSourceFilesFromSources(targetPath, SourceCode);
                foreach (var source in tempSources)
                {
                    parameters.TempFiles.AddFile(source, false);
                    sources.Add(source);
                }
            }

            MetadataReferences.Keys.All((reference) => { return parameters.ReferencedAssemblies.Add(reference) >= 0; });

            var provider = CSharpCodeProvider.CreateProvider("CSharp");
            var compilerResult = provider.CompileAssemblyFromFile(parameters, sources.ToArray());
            try
            {
                if (compilerResult.Errors.HasErrors)
                {
                    RaiseCompilationError(compilerResult);
                }
            }
            finally
            {
                SafeDeleteTempFiles(compilerResult);
            }

            var privateReferences = MetadataReferences.Where(kv => kv.Value == true).Select(item => item.Key);
            CopyAndRegisterPrivateAssemblies(privateReferences, result.OutputDirectory, result);

            return result;
        }

        string CreateTempDirectory()
        {
            var guid = Guid.NewGuid().ToString();
            var tp = Path.GetTempPath();
            var tempPath = Path.Combine(tp, guid);
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
            Directory.CreateDirectory(tempPath);
            return tempPath;
        }

        IEnumerable<string> CreateSourceFilesFromSources(string directory, IEnumerable<string> sources)
        {
            var result = new List<string>(sources.Count());
            var baseName = Guid.NewGuid().ToString();
            int counter = 0;

            foreach (var source in sources)
            {
                var written = false;
                do
                {
                    var name = $"{baseName}.{counter}.cs";
                    var fullName = Path.Combine(directory, name);
                    counter++;

                    if (!File.Exists(fullName))
                    {
                        File.WriteAllText(fullName, source);
                        result.Add(fullName);
                        written = true;
                    }
                }
                while (!written);
            }

            return result;
        }

        void RaiseCompilationError(CompilerResults result)
        {
            throw new AppCompilerException(result);
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

        void CopyAndRegisterPrivateAssemblies(IEnumerable<string> files, string directory, AppCompilerResult result, bool overwrite = true)
        {
            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var target = Path.Combine(directory, name);

                File.Copy(file, target, overwrite);

                result.PrivateReferenceAssemblies.Add(target);
            }
        }
    }
}
