using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Convienence methods extending the <c>AppCompiler</c>.
    /// </summary>
    public static class AppCompilerExtensions
    {
        public static AppCompiler WithDefaultReferences(this AppCompiler compiler, string starcounterBinDirectory = null)
        {
            starcounterBinDirectory = starcounterBinDirectory ?? StarcounterEnvironment.InstallationDirectory;

            var publicAssemblies = Path.Combine(starcounterBinDirectory, "Public Assemblies");
            
            // Keep these in sync with our VS templates to create a uniform experience.
            
            var defaultStarcounterReferences = new[]
            {
                "Starcounter.dll",
                "Starcounter.Bootstrap.dll",
                "Starcounter.Internal.dll",
                "Starcounter.XSON.dll"
            };

            compiler.AddReferencesFromDirectory(publicAssemblies, defaultStarcounterReferences);

            var defaultGACAssemblies = new[]
            {
                "System",
                "System.Core",
                "System.Xml",
                "System.Xml.Linq",
                "System.Data",
                "System.Data.DataSetExtensions",
                "Microsoft.CSharp"
            };

            compiler.AddGACReferences(defaultGACAssemblies);

            return compiler;
        }

        public static AppCompiler WithReference(this AppCompiler compiler, string assemblyReference, bool copyLocal = true)
        {
            Guard.NotNullOrEmpty(assemblyReference, nameof(assemblyReference));

            var directory = Path.GetDirectoryName(assemblyReference);
            var file = Path.GetFileName(assemblyReference);

            compiler.AddReferenceFromDirectory(directory, file, copyLocal);

            return compiler;
        }
        
        public static AppCompiler WithSourceCode(this AppCompiler compiler, string sourceCode)
        {
            if (sourceCode == null)
            {
                throw new ArgumentNullException(nameof(sourceCode));
            }

            compiler.SourceCode.Add(sourceCode);
            return compiler;
        }

        public static AppCompiler WithSourceCodeFile(this AppCompiler compiler, string sourceFile)
        {
            if (string.IsNullOrEmpty(sourceFile))
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException(sourceFile);
            }

            compiler.SourceFiles.Add(sourceFile);
            return compiler;
        }

        public static AppCompiler WithStarcounterAssemblyInfo(this AppCompiler compiler, string starcounterBinDirectory = null)
        {
            starcounterBinDirectory = starcounterBinDirectory ?? StarcounterEnvironment.InstallationDirectory;

            var configDir = "cli-config";
            var assemblyInfoFile = "StarcounterAssembly.cs";
            var file = Path.Combine(StarcounterEnvironment.InstallationDirectory, configDir, assemblyInfoFile);
            return compiler.WithSourceCodeFile(file);
        }

        static void AddReferencesToAllAssembliesInDirectory(this AppCompiler compiler, string directory, bool copyLocal = true)
        {
            var libraries = Directory.EnumerateFiles(directory, "*.dll");
            compiler.AddReferencesFromDirectory(directory, libraries, copyLocal);
        }

        static void AddReferencesFromDirectory(this AppCompiler compiler, string directory, IEnumerable<string> assemblies, bool copyLocal = true)
        {
            foreach (var assembly in assemblies)
            {
                compiler.AddReferenceFromDirectory(directory, Path.GetFileName(assembly), copyLocal);
            }
        }

        static void AddReferenceFromDirectory(this AppCompiler compiler, string directory, string assemblyName, bool copyLocal = true)
        {
            var reference = Path.Combine(directory, WithDefaultReferenceExtension(assemblyName));
            if (!File.Exists(reference))
            {
                throw new FileNotFoundException($"Unable to add reference to {assemblyName}; file {reference} does not exist");
            }

            compiler.AddReference(reference, copyLocal);
        }

        static void AddGACReferences(this AppCompiler compiler, IEnumerable<string> assemblies)
        {
            foreach (var assembly in assemblies)
            {
                compiler.AddGACReference(assembly);
            }
        }

        static void AddGACReference(this AppCompiler compiler, string assemblyName)
        {
            compiler.AddReference(WithDefaultReferenceExtension(assemblyName), false);
        }

        static void AddReference(this AppCompiler compiler, string verifiedReference, bool copyLocal = true)
        {
            if (!compiler.MetadataReferences.ContainsKey(verifiedReference))
            {
                compiler.MetadataReferences.Add(verifiedReference, copyLocal);
            }
        }

        static string WithDefaultReferenceExtension(string assemblyName)
        {
            var ignoreCase = StringComparison.InvariantCultureIgnoreCase;
            var extension = Path.GetExtension(assemblyName);

            if (string.IsNullOrEmpty(extension) || (!extension.Equals(".dll", ignoreCase) && !extension.Equals(".exe", ignoreCase)))
            {
                assemblyName += ".dll";
            }

            return assemblyName;
        }
    }
}
