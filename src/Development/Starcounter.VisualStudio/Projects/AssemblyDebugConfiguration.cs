
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Starcounter.CommandLine;

namespace Starcounter.VisualStudio.Projects {
    using ProjectConfiguration = StarcounterProjectConfiguration;
    using ConfigurationProperty = StarcounterProjectConfiguration.PropertyNames;
    using Starcounter.Internal;

    /// <summary>
    /// Maintains the subset of configuration that is interesting to
    /// the Visual Studio extensions when <see cref="StarcounterProjectConfiguration"/>
    /// are being launched/debugged.
    /// </summary>
    internal sealed class AssemblyDebugConfiguration {
        string outputType;
        string startAction;
        string startArgumentsString;

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

        internal string[] Arguments {
            get {
                return string.IsNullOrEmpty(startArgumentsString)
                    ? new string[] { }
                    : CommandLineStringParser.SplitCommandLine(startArgumentsString).ToArray();
            }
        }

        internal bool IsSelfHosted {
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
                targetAssembly = targetAssembly ?? string.Empty;
                var path = string.Format("Path: {0}", targetAssembly);
                throw ErrorCode.ToException(
                    Error.SCERRBINARYNOTFOUNDWHENDEBUG, path, (s, e) => { return new FileNotFoundException(s, e); });
            }
            this.AssemblyPath = targetAssembly;

            var selfHosted = false;
            var selfHostedCfg = cfg.GetPropertyValue(ConfigurationProperty.SelfHosted);
            if (!string.IsNullOrEmpty(selfHostedCfg))
            {
                selfHosted = bool.Parse(selfHostedCfg);
            }

            var targetDirectory = Path.GetDirectoryName(targetAssembly);
            var workingDirectory = cfg.GetPropertyValue(ConfigurationProperty.WorkingDirectory);
            if (string.IsNullOrEmpty(workingDirectory)) {
                workingDirectory = targetDirectory;
            }
            else {
                workingDirectory = Path.Combine(targetDirectory, workingDirectory);
            }

            this.WorkingDirectory = workingDirectory;
            this.IsSelfHosted = selfHosted;
            this.startArgumentsString = cfg.GetPropertyValue(ConfigurationProperty.StartArguments);
        }
    }
}
