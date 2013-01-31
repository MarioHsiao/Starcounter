using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.InstallerEngine.VsSetup
{
    /// <summary>
    /// Represent metadata about a VSIX extension package file (.vsix),
    /// consumed by the installer.
    /// </summary>
    internal class VSIXPackageInfo
    {
        /// <summary>
        /// Gets the extension metadata for the VS 2010 package file.
        /// </summary>
        internal static readonly VSIXPackageInfo VS2010 = new VSIXPackageInfo("Starcounter.VisualStudio.10.0.vsix", "c8636ce1-46b5-4e7c-a558-4925ea1ed3ac");

        /// <summary>
        /// Gets the extension metadata for the VS 2012 package file.
        /// </summary>
        internal static readonly VSIXPackageInfo VS2012 = new VSIXPackageInfo("Starcounter.VisualStudio.11.0.vsix", "Starcounter.VS11.Extension.c3ff293a-4564-4f91-8fb8-635a64f6b310");

        /// <summary>
        /// Private constructor initializing a <see cref="VSIXPackageInfo"/>.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="extensionIdentity"></param>
        private VSIXPackageInfo(string fileName, string extensionIdentity)
        {
            this.FileName = fileName;
            this.ExtensionIdentity = extensionIdentity;
        }

        /// <summary>
        /// The simple file name of the extension package file (expected to
        /// include it's ".vsixmanifest" file extension).
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// The unique identity used to represent the extension.
        /// </summary>
        /// <remarks>
        /// The master copy of this value is in each respective .vsixmanifest file, so
        /// make sure to keep the in sync if they ever have to change.
        /// </remarks>
        public readonly string ExtensionIdentity;
    }
}
