
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;

namespace Starcounter.VisualStudio.Projects {
    
    [ComVisible(false)]
    internal class AppExeProjectConfiguration : StarcounterProjectConfiguration {
        /// <summary>
        /// The names of the project properties utilized by this class.
        /// </summary>
        static class PropertyNames {
            internal const string StartAction = "StartAction";
            internal const string AssemblyPath = "TargetPath";
            internal const string WorkingDirectory = "StartWorkingDirectory";
            internal const string StartArguments = "StartArguments";
        }

        public AppExeProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration)
            : base(package, project, baseConfiguration, innerConfiguration) {
        }

        protected override void DefineSupportedProperties(Dictionary<string, ProjectPropertySettings> properties) {
            base.DefineSupportedProperties(properties);
            properties[PropertyNames.StartAction] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true, ProjectStartAction.Project);
            properties[PropertyNames.AssemblyPath] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
            properties[PropertyNames.WorkingDirectory] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
            properties[PropertyNames.StartArguments] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
        }

        protected override bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        protected override bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            
            // Do this:
            // 1. Get the configured start action. Currently, we support only
            // the project start action.

            if (!string.Equals(
                GetPropertyValue(PropertyNames.StartAction), 
                ProjectStartAction.Project, StringComparison.InvariantCultureIgnoreCase)) {
                throw new NotSupportedException("Only 'Project' start action is currently supported.");
            }

            // 2. Get the state we need: at a very minimum, the path to the executable.
            // Also query the command-line and the working directory.

            var targetAssembly = GetPropertyValue(PropertyNames.AssemblyPath);
            if (string.IsNullOrEmpty(targetAssembly) || !File.Exists(targetAssembly)) {
                throw new FileNotFoundException("Unable to find assembly to run.", targetAssembly);
            }
            var workingDirectory = GetPropertyValue(PropertyNames.WorkingDirectory);
            if (string.IsNullOrEmpty(workingDirectory)) {
                workingDirectory = Path.GetDirectoryName(targetAssembly);
            }
            var arguments = GetPropertyValue(PropertyNames.StartArguments);

            // 3. Ask the server to exec the executable. Just the same as when an app
            // starts up (which must change for this to work, using the standard named pipes).
            // TODO:

            // 4. Get back the process identity of the database in which the app now
            // runs.
            // TODO:

            // 5. Attatch the debugger to that process (i.e. the database).
            // TODO:


            throw new NotImplementedException();
        }
    }
}