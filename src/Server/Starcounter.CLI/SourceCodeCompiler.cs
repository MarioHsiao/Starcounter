using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.IO;
using Starcounter.Internal;

namespace Starcounter.CLI {
    /// <summary>
    /// Implements the source code compiling capabilities of our
    /// CLI tools.
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
    public class SourceCodeCompiler {
        static string starcounterAssembliesFolder = StarcounterEnvironment.InstallationDirectory;
        static string starcounterDatabaseClassesFolder = StarcounterEnvironment.LibrariesWithDatabaseClassesDirectory;

        static string[] DefaultAssemblyReferences = new string[] {
            "Starcounter",
            "Starcounter.Apps.JsonPatch",
            "Starcounter.HyperMedia",
            "Starcounter.Internal",
            "Starcounter.Logging",
            "Starcounter.XSON",
            "System",
            "System.Core",
            "System.Xml",
            "System.Xml.Linq",
            "System.Data",
            "System.Data.DataSetExtensions",
            "Microsoft.CSharp"
        };

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
                GenerateInMemory = false,
                IncludeDebugInformation = true
            };

            // Create a unique directory under temp, one which we will later
            // delete when cleaning up

            var guid = Guid.NewGuid().ToString();
            var tempPath = Path.GetTempPath();
            tempPath = Path.Combine(tempPath, guid);
            if (Directory.Exists(tempPath)) {
                Directory.Delete(tempPath, true);
            }
            Directory.CreateDirectory(tempPath);

            parameters.TempFiles = new TempFileCollection(tempPath, false);

            var temporaryDiskExePath = Path.GetFileNameWithoutExtension(sourceCode);
            temporaryDiskExePath += ".exe";
            temporaryDiskExePath = Path.Combine(parameters.TempFiles.TempDir, temporaryDiskExePath);
            try {
                temporaryDiskExePath = Path.GetFullPath(temporaryDiskExePath);
            } catch (PathTooLongException) {
                // We should provide a nice error here already, saying that either
                // the user can use another temp directory (shorter path), or have
                // a shorter file name.
                // TODO:
                throw;
            }

            parameters.TempFiles.AddFile(temporaryDiskExePath, true);
            parameters.TempFiles.AddFile(Path.ChangeExtension(temporaryDiskExePath, ".pdb"), true);

            parameters.OutputAssembly = temporaryDiskExePath;
            
            // As for assemblies, we'll start with a strategy where we add
            // all Starcounter assemblies comprising the default reference set
            // for a Starcounter application project, and all System assemblies
            // part of a standard Console Window dito.
            //
            // As a future feature, we should add the ability to specify the
            // parameters. Just support some CLI option that allows a reference
            // to be given, and pass the value along to the same method as used
            // here.

            foreach (var reference in DefaultAssemblyReferences) {
                AddDefaultAssemblyReference(parameters, reference);
            }

            // Add additional references from known class library folders.
            // This can easily be extended later, to allow for more folders to
            // be passed on the command-line or in a reference config file.

            var additionalClassLibraryFolders = new[] { starcounterDatabaseClassesFolder };
            foreach (var libFolder in additionalClassLibraryFolders) {
                AddAssemblyReferencesFromDirectory(parameters, libFolder);
            }

            var result = provider.CompileAssemblyFromFile(parameters, sourceCode);
            if (result.Errors.Count > 0) {
                assemblyPath = null;

                var headline = string.Format("Compilation errors ({0}):", result.Errors.Count);
                Console.WriteLine(headline);
                foreach (CompilerError error in result.Errors) {
                    WriteCompilationError(error);
                }
                throw new Exception("Errors compiling!");
            }

            // Clean up all temporary files produced that are still
            // around and marked OK to delete; our executable will
            // is marked to remain. We'll delete it after we have
            // executed.

            result.TempFiles.Delete();

            assemblyPath = result.PathToAssembly;
        }

        static void AddDefaultAssemblyReference(CompilerParameters parameters, string assemblyName) {
            if (!assemblyName.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)) {
                assemblyName += ".dll";
            }

            var candidate = Path.Combine(starcounterAssembliesFolder, assemblyName);
            if (File.Exists(candidate)) {
                assemblyName = candidate;
            }

            if (!parameters.ReferencedAssemblies.Contains(assemblyName)) {
                parameters.ReferencedAssemblies.Add(assemblyName);
            }
        }

        static void AddAssemblyReferencesFromDirectory(CompilerParameters parameters, string classLibraryDir) {
            var dir = new DirectoryInfo(classLibraryDir);
            var files = dir.GetFiles("*.dll");

            foreach (var lib in files) {
                if (!parameters.ReferencedAssemblies.Contains(lib.FullName)) {
                    parameters.ReferencedAssemblies.Add(lib.FullName);
                }
            }
        }

        static void WriteCompilationError(CompilerError error) {
            var s = new StringBuilder()
            .Append(Path.GetFileName(error.FileName))
            .AppendFormat("({0},{1}): ", error.Line, error.Column)
            .AppendFormat("{0} {1}: ", error.IsWarning ? "warning" : "error", error.ErrorNumber)
            .Append(error.ErrorText).ToString();
            Console.WriteLine(s);
        }
    }
}
