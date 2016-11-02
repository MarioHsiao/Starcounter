
using System;
using System.IO;

namespace Starcounter.Server.Compiler
{
    /// <summary>
    /// Define the result of <c>AppCompiler.Compile()</c>.
    /// </summary>
    public sealed class AppCompilerResult
    {
        readonly string appName;
        
        /// <summary>
        /// Full path to directory where the compiled app reside.
        /// </summary>
        public string OutputDirectory { get; private set; }

        /// <summary>
        /// Full path to the compiled application executable.
        /// </summary>
        public string ApplicationPath {
            get {
                return Path.Combine(OutputDirectory, $"{appName}.exe");
            }
        }

        /// <summary>
        /// Full path to the application symbol file.
        /// </summary>
        public string SymbolFilePath {
            get {
                return Path.Combine(OutputDirectory, $"{appName}.pdb");
            }
        }

        internal AppCompilerResult(string name, string directory)
        {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrEmpty(directory)) {
                throw new ArgumentNullException(nameof(directory));
            }

            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"Directory {directory} dont exist");
            }

            appName = name;
            OutputDirectory = directory;
        }
    }
}
