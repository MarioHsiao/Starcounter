﻿
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Project = EnvDTE.Project;

namespace Starcounter.VisualStudio.Projects {
    internal abstract class StarcounterProjectConfiguration :
        IVsProjectFlavorCfg,
        IVsDebuggableProjectCfg {
        /// <summary>
        /// The configuration name. Stored in the form Configuration|Platform.
        /// Will typically be either "Debug|AnyCPU" or "Release|AnyCPU", provided
        /// that other configurations/platforms have not been added to the project
        /// this configuration belongs to.
        /// </summary>
        private readonly string configName;

        private IVsHierarchy hierarchy;
        private IVsProjectFlavorCfg innerCfg;

        protected readonly VsPackage package;
        protected string debugLaunchDescription;
        protected bool debugLaunchPending;
        protected IVsProjectCfg baseCfg;

        public Project Project {
            get;
            private set;
        }

        protected StarcounterProjectConfiguration(VsPackage package, IVsHierarchy project, IVsProjectCfg baseConfiguration, IVsProjectFlavorCfg innerConfiguration) {
            // Make sure we use the outer part of the aggregation
            IntPtr projectUnknown = Marshal.GetIUnknownForObject(project);
            object projectObj;

            try {
                this.hierarchy = (IVsHierarchy)Marshal.GetTypedObjectForIUnknown(projectUnknown, typeof(IVsHierarchy));
            } finally {
                if (projectUnknown != IntPtr.Zero) {
                    Marshal.Release(projectUnknown);
                }
            }

            this.package = package;
            this.baseCfg = baseConfiguration;
            this.innerCfg = innerConfiguration;
            this.baseCfg.get_DisplayName(out this.configName);
            ErrorHandler.ThrowOnFailure(this.hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectObj));
            this.Project = (Project)projectObj;
        }

        #region Primary method set we expect implementations to customize

        protected virtual bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        #endregion

        #region Shared debugging code, to enable debugging of projects

        /// <summary>
        /// When implemented in a subclass, begins debugging.
        /// </summary>
        /// <param name="deployUI"></param>
        /// <returns></returns>
        bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            throw new NotImplementedException();
        }

        #endregion

        #region Utility methods

        public Microsoft.Build.Evaluation.Project GetBuildProject() {
            ICollection<Microsoft.Build.Evaluation.Project> projects;
            Microsoft.Build.Evaluation.Project project;

            projects = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(this.Project.FullName);
            if (projects == null || projects.Count != 1)
                return null;

            project = null;
            foreach (var p in projects) {
                project = p;
                break;
            }

            return project;
        }

        protected void ReportError(string text, params object[] parameters) {
            ReportError(string.Format(text, parameters));
        }

        protected void ReportError(string description) {
            var task = package.ErrorList.NewTask(ErrorTaskSource.Debug);
            task.Text = description;

            this.package.ErrorList.Tasks.Add(task);
            this.package.ErrorList.Refresh();
            this.package.ErrorList.Show();
        }

        protected void WriteDebugLaunchStatus(string status) {
            string text;

            if (string.IsNullOrEmpty(this.debugLaunchDescription) && status == null) {
                text = null;
            } else {
                status = status ?? "please wait...";
                text = string.Format("{0} ({1})", this.debugLaunchDescription, status);
            }

            this.package.WriteStatusText(text);
        }

        #endregion

        #region IVsDebuggableProjectCfg members we actually implement (to support debugging)

        int IVsDebuggableProjectCfg.QueryDebugLaunch(uint grfLaunch, out int pfCanLaunch) {
            bool ready = CanBeginDebug((__VSDBGLAUNCHFLAGS)grfLaunch);
            ready = ready && (debugLaunchPending ? false : true);
            pfCanLaunch = ready ? 1 : 0;
            return VSConstants.S_OK;
        }

        int IVsDebuggableProjectCfg.DebugLaunch(uint grfLaunch) {
            bool launchResult;

            // Keep an eye on the status of the process launching
            // the Debug command. If we need to do something lengthy in
            // it, we might have several invocations of this
            // command and we should make sure just to process
            // one (the first).
            //
            // Should we use this to enable/disable debugging to be
            // triggered in the QueryDebug method? Investigate how the
            // IDE behaves by simulating a lengthy wait for the
            // database to start. Check if it can be cancelled.
            //
            // TODO:

            if (debugLaunchPending) return VSConstants.S_OK;

            debugLaunchPending = true;
            debugLaunchDescription = "";

            try {
                launchResult = BeginDebug((__VSDBGLAUNCHFLAGS)grfLaunch);
            } catch (Exception unexpectedException) {
                // Don't let exceptions slip throuh to the IDE and make
                // sure we restore the debug launch pending control flag.

                this.ReportError(
                    "Unexpected exception when trying to run the debugging launch sequence: {0}", unexpectedException.Message);
                launchResult = false;
            }

            if (launchResult == false) {
                debugLaunchPending = false;
                debugLaunchDescription = "";
            }

            return launchResult ? VSConstants.S_OK : VSConstants.S_FALSE;
        }

        #endregion

        #region IVsProjectFlavorCfg Members

        /// <summary>
        /// If we support the interface, provide an object that implements it.
        /// </summary>
        /// <param name="iidCfg">IID of the interface that is being asked</param>
        /// <param name="ppCfg">Object that implement the interface</param>
        /// <returns>HRESULT</returns>
        int IVsProjectFlavorCfg.get_CfgType(ref Guid iidCfg, out IntPtr ppCfg) {
            ppCfg = IntPtr.Zero;
            // Currently, we commit to being a debuggable project. That is, we
            // instruct VS to show the standard F5/Start command.
            if (iidCfg == typeof(IVsDebuggableProjectCfg).GUID) {
                ppCfg = Marshal.GetComInterfaceForObject(this, typeof(IVsDebuggableProjectCfg));
            }

            // Allow this type to be further extended, and hence make sure we do
            // propagate the request to eventual inner projects if we didn't respond
            // to it ourself.
            if (ppCfg == IntPtr.Zero) {
                if (innerCfg != null) {
                    // Ask inner flavor if it support this
                    return innerCfg.get_CfgType(ref iidCfg, out ppCfg);
                } else {
                    return VSConstants.E_NOINTERFACE;
                }
            }

            return VSConstants.S_OK;
        }

        int IVsProjectFlavorCfg.Close() {
            if (this.hierarchy != null) {
                this.hierarchy = null;
            }
            if (this.baseCfg != null) {
                if (Marshal.IsComObject(this.baseCfg)) {
                    Marshal.ReleaseComObject(this.baseCfg);
                }
                this.baseCfg = null;
            }
            if (this.innerCfg != null) {
                if (Marshal.IsComObject(this.innerCfg)) {
                    Marshal.ReleaseComObject(this.innerCfg);
                }
                this.innerCfg = null;
            }
            return VSConstants.S_OK;
        }

        #endregion

        #region Not supported interface implementations

        int IVsCfg.get_DisplayName(out string pbstrDisplayName) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_IsDebugOnly(out int pfIsDebugOnly) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_IsReleaseOnly(out int pfIsReleaseOnly) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_CanonicalName(out string pbstrCanonicalName) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_Platform(out Guid pguidPlatform) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_IsPackaged(out int pfIsPackaged) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_TargetCodePage(out uint puiTargetCodePage) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_UpdateSequenceNumber(ULARGE_INTEGER[] puliUSN) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_RootURL(out string pbstrRootURL) {
            throw new NotSupportedException();
        }

        int IVsDebuggableProjectCfg.get_DisplayName(out string pbstrDisplayName) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_IsDebugOnly(out int pfIsDebugOnly) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_IsReleaseOnly(out int pfIsReleaseOnly) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_CanonicalName(out string pbstrCanonicalName) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_Platform(out Guid pguidPlatform) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_IsPackaged(out int pfIsPackaged) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_TargetCodePage(out uint puiTargetCodePage) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_UpdateSequenceNumber(ULARGE_INTEGER[] puliUSN) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_RootURL(out string pbstrRootURL) {
            throw new NotSupportedException();
        }

        int IVsProjectCfg.get_DisplayName(out string pbstrDisplayName) {
            throw new NotSupportedException();
        }

        int IVsCfg.get_IsDebugOnly(out int pfIsDebugOnly) {
            throw new NotSupportedException();
        }

        int IVsCfg.get_IsReleaseOnly(out int pfIsReleaseOnly) {
            throw new NotSupportedException();
        }

        #endregion
    }
}