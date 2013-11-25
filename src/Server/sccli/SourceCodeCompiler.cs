﻿using System;
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
        static string starcounterAssembliesFolder = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        static string[] DefaultAssemblyReferences = new string[] {
            "Starcounter",
            "Starcounter.Apps.JsonPatch",
            "Starcounter.HyperMedia",
            "Starcounter.Internal",
            "Starcounter.Logging",
            "Starcounter.Node",
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
                GenerateInMemory = false
            };

            /*
             * Backlog before 530-push.
             * 
             * A)
               make a nicer/minimal output when running scripts!

            B)
            Fix temporary storage compiler (and delete file).
            Change so that we delete the temporary files from
            the compiler!

            C)
            Fix always restart w/ scripts
             */


            parameters.TempFiles = new TempFileCollection(Path.GetTempPath(), false);
            
            var temporaryDiskExePath = Path.GetRandomFileName();
            temporaryDiskExePath += Guid.NewGuid().ToString();
            temporaryDiskExePath += ".exe";
            temporaryDiskExePath = Path.Combine(parameters.TempFiles.TempDir, temporaryDiskExePath);
            parameters.TempFiles.AddFile(temporaryDiskExePath, true);

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
                AddAssemblyReference(parameters, reference);
            }

            var result = provider.CompileAssemblyFromFile(parameters, sourceCode);
            if (result.Errors.Count > 0) {
                // Improved error handling
                // TODO:
                assemblyPath = null;
                foreach (var error in result.Errors) {
                    Console.WriteLine(error.ToString());
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

        static void AddAssemblyReference(CompilerParameters parameters, string assemblyName) {
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
    }
}
