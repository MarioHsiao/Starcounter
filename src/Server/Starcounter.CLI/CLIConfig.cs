using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.CLI {
    /// <summary>
    /// Facade to CLI configuration
    /// </summary>
    public sealed class CLIConfig {
        /// <summary>
        /// Provides the name of the folder storing CLI configuration / assets.
        /// </summary>
        public const string FolderName = "cli-config";
        /// <summary>
        /// Gets the name of the assembly directive file.
        /// </summary>
        public const string AssemblyDirectivesFile = "StarcounterAssembly.cs";

        /// <summary>
        /// Gets the full resolved path to the CLI configuration.
        /// </summary>
        public static string FullPath {
            get { return Path.Combine(StarcounterEnvironment.InstallationDirectory, FolderName); }
        }
    }
}
