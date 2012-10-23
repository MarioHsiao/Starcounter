
using Microsoft.VisualStudio.Shell.Flavor;
using System;
using System.Runtime.InteropServices;

namespace Starcounter.VisualStudio.Projects {
    
    /// <summary>
    /// Project factory for file app exe projects.
    /// </summary>
    [ComVisible(false)]
    [GuidAttribute(VsPackageConstants.AppExeProjectGUIDString)]
    public class AppExeProjectFactory : FlavoredProjectFactoryBase {
        private readonly VsPackage package;

        /// <summary>
        /// Initializes the project factory.
        /// </summary>
        /// <remarks>
        /// Called by Visual Studio when it needs to create an
        /// instance of an app exe project.
        /// </remarks>
        /// <param name="package"></param>
        public AppExeProjectFactory(VsPackage package) {
            this.package = package;
        }

        /// <summary>
        /// Creates an instance of an app exe project.
        /// </summary>
        /// <param name="outerProjectIUnknown"></param>
        /// <returns></returns>
        protected override object PreCreateForOuter(IntPtr outerProjectIUnknown) {
            return new AppExeProject(this.package);
        }
    }
}