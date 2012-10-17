
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Starcounter.VisualStudio.Projects {
    using ProjectConfiguration = StarcounterProjectConfiguration;
    using ConfigurationProperty = StarcounterProjectConfiguration.PropertyNames;

    /// <summary>
    /// Maintains the subset of configuration that is interesting to
    /// the Visual Studio extensions when <see cref="StarcounterProjectConfiguration"/>
    /// are being launched/debugged.
    /// </summary>
    internal sealed class AssemblyDebugConfiguration {
        string outputType;
        string startAction;

        /// <summary>
        /// Defines the set of output types we can allow.
        /// </summary>
        internal static class OutputType {
            internal const string ConsoleApplication = "Exe";
            internal const string WindowsExe = "WinExe";
            internal const string Library = "Library";
        }

        /// <summary>
        /// Gets the path to the target assembly, i.e. the artifact that
        /// was built.
        /// </summary>
        internal string AssemblyPath {
            get;
            private set;
        }

        internal string WorkingDirectory {
            get;
            private set;
        }

        internal bool IsConsoleApplication {
            get {
                return this.outputType.Equals(OutputType.ConsoleApplication);
            }
        }

        internal bool IsWindowsApplication {
            get {
                return this.outputType.Equals(OutputType.WindowsExe);
            }
        }

        internal bool IsLibrary {
            get {
                return this.outputType.Equals(OutputType.Library);
            }
        }

        internal bool IsStartProject {
            get {
                return this.startAction.Equals(ProjectStartAction.Project);
            }
        }

        internal bool IsStartExternalProgram {
            get {
                return this.startAction.Equals(ProjectStartAction.ExternalProgram);
            }
        }

        internal bool IsStartURL {
            get {
                return this.startAction.Equals(ProjectStartAction.URL);
            }
        }

        internal AssemblyDebugConfiguration(ProjectConfiguration projectConfiguration) {
            Initialize(projectConfiguration);
        }

        void Initialize(ProjectConfiguration cfg) {
            outputType = cfg.GetPropertyValue(ConfigurationProperty.OutputType);
            startAction = cfg.GetPropertyValue(ConfigurationProperty.StartAction);

            var targetAssembly = cfg.GetPropertyValue(ConfigurationProperty.AssemblyPath);
            if (string.IsNullOrEmpty(targetAssembly) || !File.Exists(targetAssembly)) {
                throw new FileNotFoundException("Unable to find assembly to run.", targetAssembly);
            }
            this.AssemblyPath = targetAssembly;

            var workingDirectory = cfg.GetPropertyValue(ConfigurationProperty.WorkingDirectory);
            if (string.IsNullOrEmpty(workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(targetAssembly);
            }
            this.WorkingDirectory = workingDirectory;

            // Parse start arguments properly, and be sure we can send them
            // through the infrastructure.
            // TODO:

            var arguments = cfg.GetPropertyValue(ConfigurationProperty.StartArguments);
        }
    }
}
