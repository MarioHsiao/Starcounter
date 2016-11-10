
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Project = EnvDTE.Project;
using Starcounter.Internal;

namespace Starcounter.VisualStudio.Projects {
    internal abstract class StarcounterProjectConfiguration :
        IVsProjectFlavorCfg,
        IVsDebuggableProjectCfg {
        /// <summary>
        /// The names of the project properties utilized by this class.
        /// </summary>
        internal static class PropertyNames {
            internal const string OutputType = "OutputType";
            internal const string StartAction = "StartAction";
            internal const string AssemblyPath = "TargetPath";
            internal const string WorkingDirectory = "StartWorkingDirectory";
            internal const string StartArguments = "StartArguments";
            internal const string SelfHosted = "SelfHosted";
        }

        /// <summary>
        /// The configuration name. Stored in the form Configuration|Platform.
        /// Will typically be either "Debug|AnyCPU" or "Release|AnyCPU", provided
        /// that other configurations/platforms have not been added to the project
        /// this configuration belongs to.
        /// </summary>
        private readonly string configName;

        private IVsHierarchy hierarchy;
        private IVsProjectFlavorCfg innerCfg;
        private readonly IVsBuildPropertyStorage buildPropertyStorage;
        private readonly Dictionary<string, ProjectPropertySettings> supportedProperties;
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
            this.supportedProperties = new Dictionary<string, ProjectPropertySettings>();
            this.baseCfg.get_DisplayName(out this.configName);
            this.buildPropertyStorage = (IVsBuildPropertyStorage)Marshal.GetTypedObjectForIUnknown(projectUnknown, typeof(IVsBuildPropertyStorage));
            ErrorHandler.ThrowOnFailure(this.hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out projectObj));
            this.Project = (Project)projectObj;
            this.DefineSupportedProperties(this.supportedProperties);
        }

        #region Primary method set we expect implementations to customize

        protected virtual bool CanBeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
        }

        /// <summary>
        /// When implemented in a subclass, begins debugging.
        /// </summary>
        /// <param name="flags">Debugging flags from the environment.</param>
        /// <returns>True if the launch of the debugger was a success; false
        /// if not.</returns>
        protected virtual bool BeginDebug(__VSDBGLAUNCHFLAGS flags) {
            return true;
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

        protected void ReportError(string text, uint code = Error.SCERRUNSPECIFIED, params object[] parameters) {
            ReportError(string.Format(text, parameters), code);
        }

        protected void ReportError(string description, uint code = Error.SCERRUNSPECIFIED) {
            var task = package.ErrorList.NewTask(ErrorTaskSource.Debug, description, code);
            
            this.package.ErrorList.Tasks.Add(task);
            this.package.ErrorList.Refresh();
            this.package.ErrorList.Show();
        }

        protected void ReportError(ErrorMessage msg) {
            var task = package.ErrorList.NewTask(ErrorTaskSource.Debug, msg);

            this.package.ErrorList.Tasks.Add(task);
            this.package.ErrorList.Refresh();
            this.package.ErrorList.Show();
        }

        protected virtual void WriteDebugLaunchStatus(string status) {
            string text;

            if (string.IsNullOrEmpty(this.debugLaunchDescription) && status == null) {
                text = null;
            } else {
                status = status ?? "please wait...";
                text = string.Format("{0} ({1})", this.debugLaunchDescription, status);
            }

            this.package.WriteStatusText(text);
        }

        protected virtual void DefineSupportedProperties(Dictionary<string, ProjectPropertySettings> properties) {
            properties[PropertyNames.OutputType] = new ProjectPropertySettings(_PersistStorageType.PST_PROJECT_FILE, false);
            properties[PropertyNames.StartAction] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true, ProjectStartAction.Project);
            properties[PropertyNames.AssemblyPath] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
            properties[PropertyNames.WorkingDirectory] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
            properties[PropertyNames.StartArguments] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
            properties[PropertyNames.SelfHosted] =
                new ProjectPropertySettings(_PersistStorageType.PST_USER_FILE, true);
        }

        /// <summary>
        /// Gets a property value.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose value to retreive.</param>
        /// <returns>The value of the specified property.</returns>
        public string GetPropertyValue(string propertyName) {
            string value;
            string configurationName;
            bool found;

            var property = supportedProperties[propertyName];
            configurationName = property.IsConfigurationDependent
                ? this.configName
                : null;

            // We look first in the storage type defined on the property
            // kind, to see if the value is found on disk.

            found = this.buildPropertyStorage.GetPropertyValue(
                propertyName,
                configurationName,
                (uint)property.StorageType,
                out value) == VSConstants.S_OK;
            if (found) return value;

            // If the property indicated it's main storage was the user
            // configuration file, we query the project level file as a
            // fallback.

            if (property.StorageType == _PersistStorageType.PST_USER_FILE) {
                found = this.buildPropertyStorage.GetPropertyValue(
                    propertyName,
                    configurationName,
                    (uint)_PersistStorageType.PST_PROJECT_FILE,
                    out value)
                    == VSConstants.S_OK;

                if (found) return value;
            }

            // And if it was found in no file, we give back the default
            // as specified when the project configuration is materialized.

            return property.DefaultValue;
        }

        /// <summary>
        /// Sets a property value.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property whose value to set.</param>
        /// <param name="value">The value to assign the property.</param>
        public void SetPropertyValue(string propertyName, string value) {
            string oldValue = GetPropertyValue(propertyName);
            if (value == oldValue) {
                return;
            }

            var property = supportedProperties[propertyName];
            if (value == null) {
                ErrorHandler.ThrowOnFailure(
                    this.buildPropertyStorage.RemoveProperty(propertyName, property.IsConfigurationDependent ? this.configName : null,
                                                             (uint)property.StorageType));
            } else {
                ErrorHandler.ThrowOnFailure(
                    this.buildPropertyStorage.SetPropertyValue(propertyName, property.IsConfigurationDependent ? this.configName : null,
                                                               (uint)property.StorageType, value));
            }
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
            ErrorMessage error;
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

            // Clean the IDE to make sure we start fresh.
            debugLaunchPending = true;
            debugLaunchDescription = "";
            this.package.ErrorList.Tasks.Clear();
            this.package.ErrorList.Refresh();

            try {
                launchResult = BeginDebug((__VSDBGLAUNCHFLAGS)grfLaunch);
            }catch (TimeoutException) {
                this.ReportError((ErrorMessage)ErrorCode.ToMessage(Error.SCERRDEBUGFAILEDCONNECTTOSERVER));
                launchResult = false;
            } catch (Exception unexpectedException) {
                // Don't let exceptions slip throuh to the IDE and make
                // sure we restore the debug launch pending control flag.
                uint code;

                if (!ErrorCode.TryGetCode(unexpectedException, out code))
                    code = 0;

                if (code > 0) {
                    // Exceptions encoded by ourselves, we report not as unexpected.
                    // We use the error message at hand. We reserve a special code,
                    // "ScErrDebugFailedReported", to allow the project implementation
                    // to report errors already reported.

                    if (code != Error.SCERRDEBUGFAILEDREPORTED) {
                        if (!ErrorCode.TryGetCodedMessage(unexpectedException, out error)) {
                            error = ErrorCode.ToMessage(code);
                        }
                        this.ReportError((ErrorMessage)error);
                    }

                } else {
                    // Log this, because we have no idea of what it is. Then report the
                    // general debugging sequence error.
                    
                    error = ErrorCode.ToMessage(Error.SCERRDEBUGSEQUENCEFAILUNEXPECT, 
                        string.Format("Error summary: {0}", unexpectedException.Message));
                    this.package.LogError(
                        string.Format("{0}{1}Exception: {2}", error.ToString(), Environment.NewLine, unexpectedException.ToString()));
                    this.ReportError(error);
                }

                launchResult = false;
            } finally {
                debugLaunchPending = false;
                debugLaunchDescription = "";
                WriteDebugLaunchStatus(null);
            }

            if (!launchResult) {
                this.package.ErrorList.Refresh();
                this.package.ErrorList.BringToFront();
                this.package.ErrorList.ForceShowErrors();
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