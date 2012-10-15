
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.Projects {
    
    [ComVisible(false)]
    internal class AppExeProjectConfiguration : StarcounterProjectConfiguration {
        public AppExeProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration)
            : base(package, project, baseConfiguration, innerConfiguration) {
        }

        protected override bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }
    }
}