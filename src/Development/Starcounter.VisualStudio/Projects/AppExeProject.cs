
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

        private AppsEvents appsEvents;

        static AppExeProject() {
            try {
                projectIcon =
                    new Icon(typeof(AppExeProject).Assembly.GetManifestResourceStream("Starcounter.VisualStudio.Resources.AppExeProject.ico"));
            } catch {
            }
        }

        public AppExeProject(VsPackage package)
            : base(package) {
//                System.Diagnostics.Debugger.Launch();

                // TODO:
                // These events are not really needed for ordinary starcounter exe projects,
                // since they only concern renaming json and codebehind and trigger the Apps
                // build task. 
                appsEvents = new AppsEvents();
                appsEvents.AddEventListeners(package);
        }

        protected override StarcounterProjectConfiguration CreateProjectConfiguration(IVsCfg pBaseProjectCfg, IVsProjectFlavorCfg inner) {
            return new AppExeProjectConfiguration(this.Package, this, (IVsProjectCfg)pBaseProjectCfg, inner);
        }

        protected override Icon GetIcon() {
            return projectIcon;
        }

        protected override void Close() {
            if (appsEvents != null) {
                appsEvents.RemoveEventListeners();
            }
            base.Close();
        }
    }
}