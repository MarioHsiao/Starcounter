
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Starcounter.CLI;
using Starcounter.Server;
using Starcounter.VisualStudio.Projects;
using System;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio {

    //
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    //
    // Resources can be extracted from a VSPackage by examining the metadata without
    // running code in the VSPackage. The VSPackage is not initialized at this time,
    // so the performance hit is minimal.
    //
    [InstalledProductRegistration(
         "#22540",                   // Name of the product (resource identity).
         "#24255",                   // Description of the product (resource identity).
         "0.1",                      // Product id
         IconResourceID = 99881,     // Resource ID of the product level icon (used in splash and about)
         LanguageIndependentName = "Starcounter")    // Language independent name
    ]
    [PackageRegistration(UseManagedResourcesOnly = true)]

    //
    // This attribute informs Visual Studio that this package contains a
    // project factory that it intends to register in the Initialize method.
    //
    [ProvideProjectFactory(
        typeof(AppExeProjectFactory),
        null,
        "Starcounter app project files (*.csproj);*.csproj",
        null,
        null,
        ".\\NullPath",
        LanguageVsTemplate = "CSharp",
        TemplateGroupIDsVsTemplate = "StarcounterApplication",
        ShowOnlySpecifiedTemplatesVsTemplate = false,
        NewProjectRequireNewFolderVsTemplate = true)
    ]

    //
    // The GUID attribute here specifies the unique identity of this package,
    // needed (among other things) to register it on the host computer where
    // it will run.
    //
    [Guid(VsPackageConstants.VsPackagePkgGUIDString)]
    public sealed class VsPackage : BaseVsPackage, IVsInstalledProduct {
        // To read about the capabilities of the status bar, consult:
        //
        // http://msdn.microsoft.com/en-us/library/bb166795(VS.80).aspx
        //
        private IVsStatusbar statusBar;

        private IVsUIShell uiShell;

        /// <summary>
        /// Gets the <see cref="ErrorList"/> to use when we report
        /// errors during deployment and/or debugging.
        /// </summary>
        internal StarcounterErrorListProvider ErrorList {
            get;
            private set;
        }

        /// <summary>
        /// Gets the top-level DTE automation object.
        /// </summary>
        public DTE DTE {
            get {
                return (DTE)GetService(typeof(SDTE));
            }
        }

        protected override void Initialize() {
            base.Initialize();

            this.RegisterProjectFactory(new AppExeProjectFactory(this));
            this.uiShell = (IVsUIShell)this.GetService(typeof(SVsUIShell));
            this.statusBar = (IVsStatusbar)GetService(typeof(SVsStatusbar));
            this.ErrorList = new StarcounterErrorListProvider(this);
            HWndDispatcher.Initialize();
            AppExeProjectConfiguration.Initialize();
            SharedCLI.InitCLIContext(KnownClientContexts.VisualStudio);
        }

        public void Invoke(Action action) {
            IntPtr hWnd;
            ErrorHandler.ThrowOnFailure(this.uiShell.GetDialogOwnerHwnd(out hWnd));
            HWndDispatcher.Invoke(hWnd, action);
        }

        public IAsyncResult BeginInvoke(Action action) {
            IntPtr hWnd;
            ErrorHandler.ThrowOnFailure(this.uiShell.GetDialogOwnerHwnd(out hWnd));
            return HWndDispatcher.BeginInvoke(hWnd, action);
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            HWndDispatcher.Uninitialize();
        }
        
        #region Properties

        public IVsUIShell UiShell {
            get {
                return this.uiShell;
            }
        }
        
        public IVsOutputWindowPane DebugOutputPane {
            get {
                IVsOutputWindow outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

                Guid debugPaneGuid = VSConstants.GUID_OutWindowDebugPane;
                IVsOutputWindowPane debugPane;
                outWindow.GetPane(ref debugPaneGuid, out debugPane);

                return debugPane;
            }
        }

        #endregion

        #region Branding

        int IVsInstalledProduct.IdBmpSplash(out uint pIdBmp) {
            pIdBmp = 0;
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.IdIcoLogoForAboutbox(out uint pIdIco) {
            pIdIco = 99881;
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.OfficialName(out string pbstrName) {
            pbstrName = GetResourceString("22540");
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductDetails(out string pbstrProductDetails) {
            pbstrProductDetails = GetResourceString("24255");
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductID(out string pbstrPID) {
            pbstrPID = GetResourceString("25970");
            return VSConstants.S_OK;
        }

        #endregion

        private string GetResourceString(string resourceName) {
            string resourceValue;
            IVsResourceManager resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
            if (resourceManager == null) {
                throw new InvalidOperationException(
                    "Could not get SVsResourceManager service. Make sure that the package is sited before calling this method");
            }
            Guid packageGuid = this.GetType().GUID;
            int hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
            ErrorHandler.ThrowOnFailure(hr);
            return resourceValue;
        }

        public string GetErrorInfo() {
            string info = string.Empty;
            try {
                var result = this.uiShell.GetErrorInfo(out info);
                if (result != VSConstants.S_OK) info = string.Empty;
            } catch { /* Ingore exceptions; just get the info if available. */}
            return info;
        }

        public int ShowMessageBox(string pszText, OLEMSGBUTTON msgbtn, OLEMSGDEFBUTTON msgdefbtn, OLEMSGICON msgicon) {
            int result = 0;
            this.Invoke(() => result = this.ShowMessageBoxImpl(pszText, msgbtn, msgdefbtn, msgicon));
            return result;
        }

        public void WriteStatusText(string text) {
            WriteStatusText(text, false);
        }

        public void WriteStatusText(string text, bool highlighted) {
            WriteStatusTextImpl(text, highlighted);
        }

        void WriteStatusTextImpl(string text, bool highlighted) {
            int frozen;

            this.statusBar.IsFrozen(out frozen);

            if (frozen == 0) {
                // NOTE:
                // SetColorText only displays white text on a 
                // dark blue background. We use this to show
                // "highlighted" status text.

                if (highlighted) statusBar.SetColorText(text, 0, 0);
                else statusBar.SetText(text);
            }
        }

        [DllImport("user32")]
        extern static IntPtr SetActiveWindow(IntPtr hWnd);
        private int ShowMessageBoxImpl(string pszText, OLEMSGBUTTON msgbtn, OLEMSGDEFBUTTON msgdefbtn, OLEMSGICON msgicon) {
            IntPtr hWnd;
            ErrorHandler.ThrowOnFailure(this.uiShell.GetDialogOwnerHwnd(out hWnd));
            SetActiveWindow(hWnd);
            Guid dummy = Guid.Empty;
            int result;
            ErrorHandler.ThrowOnFailure(this.uiShell.ShowMessageBox(0, ref dummy, "Starcounter for Visual Studio", pszText, null, 0, msgbtn, msgdefbtn,
                                                                    msgicon, 0, out result));
            return result;
        }

        private IVsProject GetVsProject(string projectName) {
            IVsProject vsProject = null;
            IVsSolution solution = (IVsSolution)this.GetService(typeof(SVsSolution));
            Guid dummy = Guid.Empty;
            IEnumHierarchies projectEnum;
            ErrorHandler.ThrowOnFailure(solution.GetProjectEnum((uint)__VSENUMPROJFLAGS.EPF_ALLPROJECTS, ref dummy, out projectEnum));
            IVsHierarchy[] hierarchies = new IVsHierarchy[1];
            uint fetched = 1;
            while (fetched > 0) {
                ErrorHandler.ThrowOnFailure(projectEnum.Next(1, hierarchies, out fetched));
                if (fetched > 0) {
                    IVsProject candidateVsProject = hierarchies[0] as IVsProject;
                    if (candidateVsProject == null) {
                        continue;
                    }
                    object o;
                    hierarchies[0].GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out o);
                    if (projectName == (string)o) {
                        vsProject = candidateVsProject;
                        break;
                    }
                }
            }
            return vsProject;
        }
    }
}