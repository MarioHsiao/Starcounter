using System;

namespace Starcounter.VisualStudio {
    /// <summary>
    /// Defines a set of constants used in this package library.
    /// </summary>
    internal static class VsPackageConstants {
        /// <summary>
        /// GUID representing the Starcounter extension VsPackage itself.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Starcounter.VisualStudio.VsPackageConstants.VsPackagePkgGUIDString"/>
        /// </remarks>
        public static readonly Guid VsPackagePkgGUID = new Guid(VsPackagePkgGUIDString);

        /// <summary>
        /// GUID, in the form of a string, representing the Starcounter extension
        /// VsPackage itself.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Starcounter.VisualStudio.VsPackageConstants.VsPackagePkgGUID"/>
        /// </remarks>
        public const string VsPackagePkgGUIDString = "1EFE6CB9-CD9D-4DA7-8D30-6DDD65C82BFB";

        /// <summary>
        /// GUID, in the form of a string, representing the project type ID of
        /// app exe projects.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Starcounter.VisualStudio.VsPackageConstants.AppExeProjectGUID"/>.
        /// <see cref="Starcounter.VisualStudio.Projects.AppExeProjectFactory"/>.
        /// </remarks>
        public const string AppExeProjectGUIDString = "C86118D7-451E-4933-BFEE-A1EFDB162FD7";

        /// <summary>
        /// GUID representing the project type of app exe projects.
        /// </summary>
        /// <remarks>
        /// <seealso cref="Starcounter.VisualStudio.VsPackageConstants.AppExeProjectGUIDString"/>.
        /// <see cref="Starcounter.VisualStudio.Projects.AppExeProjectFactory"/>.
        /// </remarks>
        public static readonly Guid AppExeProjectGUID = new Guid(AppExeProjectGUIDString);
    }
}