
using Microsoft.VisualStudio.Shell.Interop;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.Projects {
    
    /// <summary>
    /// Implements the app exe project-level functionality, independent
    /// of the build configuration (i.e. ignoring Debug|Release etc).
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    internal class AppExeProject : StarcounterProject {
        private static readonly Icon projectIcon;

        static AppExeProject() {
            try {
                projectIcon =
                    new Icon(typeof(AppExeProject).Assembly.GetManifestResourceStream("Starcounter.VisualStudio.Resources.AppExeProject.ico"));
            } catch {
            }
        }

        public AppExeProject(VsPackage package)
            : base(package) {
        }

        protected override StarcounterProjectConfiguration CreateProjectConfiguration(IVsCfg pBaseProjectCfg, IVsProjectFlavorCfg inner) {
            return new AppExeProjectConfiguration(this.Package, this, (IVsProjectCfg)pBaseProjectCfg, inner);
        }

        protected override Icon GetIcon() {
            return projectIcon;
        }
    }
}