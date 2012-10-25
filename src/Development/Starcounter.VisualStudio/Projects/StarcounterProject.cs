
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Flavor;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.Projects {
    internal abstract class StarcounterProject : FlavoredProjectBase, IVsProjectFlavorCfgProvider {
        private IVsProjectFlavorCfgProvider innerVsProjectFlavorCfgProvider;

        public readonly VsPackage Package;

        protected StarcounterProject(VsPackage package) {
            this.Package = package;
        }

        #region IVsProjectFlavorCfgProvider Members

        int IVsProjectFlavorCfgProvider.CreateProjectFlavorCfg(IVsCfg pBaseProjectCfg, out IVsProjectFlavorCfg ppFlavorCfg) {
            IVsProjectFlavorCfg inner;
            StarcounterProjectConfiguration configuration;

            inner = null;

            if (innerVsProjectFlavorCfgProvider != null) {
                innerVsProjectFlavorCfgProvider.CreateProjectFlavorCfg(pBaseProjectCfg, out inner);
            }

            configuration = CreateProjectConfiguration(pBaseProjectCfg, inner);
            ppFlavorCfg = configuration;

            return VSConstants.S_OK;
        }

        #endregion

        #region FlavoredProjectBase overrides

        protected override void SetInnerProject(IntPtr innerIUnknown) {
            object inner = Marshal.GetObjectForIUnknown(innerIUnknown);
            if (base.serviceProvider == null) {
                base.serviceProvider = this.Package;
            }
            base.SetInnerProject(innerIUnknown);
            this.innerVsProjectFlavorCfgProvider = inner as IVsProjectFlavorCfgProvider;
        }

        protected override int GetProperty(uint itemId, int propertyID, out object property) {
            // Currently, we provide no customization of properties other
            // that of the icon. We'll add stuff to customize configuration
            // properties later.

            switch (propertyID) {
                case (int)__VSHPROPID.VSHPROPID_IconIndex:
                case (int)__VSHPROPID.VSHPROPID_OpenFolderIconIndex:
                    if (itemId == VSConstants.VSITEMID_ROOT) {
                        //Forward to __VSHPROPID.VSHPROPID_IconHandle and __VSHPROPID.VSHPROPID_OpenFolderIconHandle propIds
                        property = null;
                        return VSConstants.E_NOTIMPL;
                    }
                    break;
                case (int)__VSHPROPID.VSHPROPID_IconHandle:
                case (int)__VSHPROPID.VSHPROPID_OpenFolderIconHandle:
                    Icon icon = GetIcon();
                    if (itemId == VSConstants.VSITEMID_ROOT && icon != null) {
                        property = icon.Handle;
                        return VSConstants.S_OK;
                    }
                    break;
                default:
                    break;
            }

            return base.GetProperty(itemId, propertyID, out property);
        }

        protected override void Close() {
            base.Close();

            if (innerVsProjectFlavorCfgProvider != null) {
                if (Marshal.IsComObject(innerVsProjectFlavorCfgProvider)) {
                    Marshal.ReleaseComObject(innerVsProjectFlavorCfgProvider);
                }
                innerVsProjectFlavorCfgProvider = null;
            }
        }

        #endregion

        #region Abstractions we force implementing classes to override

        protected abstract StarcounterProjectConfiguration CreateProjectConfiguration(
            IVsCfg pBaseProjectCfg,
            IVsProjectFlavorCfg inner
            );

        protected abstract Icon GetIcon();

        #endregion
    }
}