
using Microsoft.VisualStudio.Shell.Interop;
using System.Drawing;
using System.Runtime.InteropServices;
using System;

namespace Starcounter.VisualStudio.Projects {
    
    /// <summary>
    /// Implements the app exe project-level functionality, independent
    /// of the build configuration (i.e. ignoring Debug|Release etc).
    /// </summary>
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    internal class AppExeProject : StarcounterProject {
        private static readonly Icon projectIcon;

        private TypedJsonEvents appsEvents;

        static AppExeProject() {
            try {
                projectIcon =
                    new Icon(typeof(AppExeProject).Assembly.GetManifestResourceStream("Starcounter.VisualStudio.Resources.AppExeProject.ico"));
            } catch {
            }
        }

        public AppExeProject(VsPackage package)
            : base(package) {
                appsEvents = new TypedJsonEvents();
                appsEvents.AddEventListeners(package);
        }

        protected override Guid GetGuidProperty(uint itemId, int propId) {
            if (propId == (int)__VSHPROPID2.VSHPROPID_AddItemTemplatesGuid) {
                return typeof(AppExeProjectFactory).GUID;
            }
            return base.GetGuidProperty(itemId, propId);
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