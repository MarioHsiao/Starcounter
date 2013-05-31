
using System;
using System.IO;
using System.Xml;

namespace Starcounter.InstallerEngine.VsSetup {
    /// <summary>
    /// Provides a set of utility methods for VSIX extension manipulation.
    /// </summary>
    public static class VSIXUtilities {
        /// <summary>
        /// Tries to find an extension manifest file containing the metadata
        /// for an extension with the identity specified in <paramref name="extensionId"/>.
        /// </summary>
        /// <param name="extensionsRootFolder">
        /// The root folder to look in for extensions.</param>
        /// <param name="extensionId">
        /// The identity to look for.</param>
        /// <returns>Full path to the manifest file if found; <c>null</c> otherwise.</returns>
        /// <remarks>
        /// <para>
        /// This method works with the conventions used by VSIX. To get an idea
        /// of these conventions, consulting http://blogs.msdn.com/b/visualstudio/archive/2010/02/19/how-vsix-extensions-are-discovered-and-loaded-in-vs-2010.aspx
        /// could be a good starting point.
        /// </para>
        /// </remarks>
        public static string FindManifestFile(string extensionsRootFolder, string extensionId) {
            if (!Directory.Exists(extensionsRootFolder)) {
                return null;
            }

            if (string.IsNullOrEmpty(extensionsRootFolder)) {
                throw new ArgumentNullException("extensionsRootFolder");
            }

            if (string.IsNullOrEmpty(extensionId)) {
                throw new ArgumentNullException("extensionId");
            }

            foreach (var extensionFolder in Directory.GetDirectories(extensionsRootFolder)) {
                var extensionManifest = Path.Combine(extensionFolder, "extension.vsixmanifest");
                if (!File.Exists(extensionManifest)) continue;
                try {
                    using (var file = new FileStream(extensionManifest, FileMode.Open)) {
                        using (var reader = XmlReader.Create(file)) {
                            while (reader.Read()) {
                                if (reader.NodeType == XmlNodeType.Element) {
                                    if (reader.Name == "Identifier") {
                                        if (reader.MoveToAttribute("Id")) {
                                            if (reader.Value.Equals(extensionId)) {
                                                return extensionManifest;
                                            }
                                        }
                                        // As soon as we've found th identifier, just break reading
                                        // and jump to the next file.
                                        break;
                                    }
                                }
                            }
                        }
                    }
                } catch {
                    // Any weird or ill-formatted extension file, we silently
                    // ignore.
                }
            }

            return null;
        }
    }
}
