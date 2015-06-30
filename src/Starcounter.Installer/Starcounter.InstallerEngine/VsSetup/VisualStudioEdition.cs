using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.InstallerEngine.VsSetup
{
    /// <summary>
    /// Represents a unique edition of a known Visual Studio version,
    /// e.g "Visual Studio 2012 Ultimate".
    /// </summary>
    public class VisualStudioEdition
    {
        /// <summary>
        /// Contains the names of some Visual Studio editions.
        /// </summary>
        public static readonly string[] NamedEditions = new string[] { "Pro", "Premium", "Ultimate" };

        /// <summary>
        /// Gets or sets the <see cref="VisualStudioVersion"/> this edition represent.
        /// </summary>
        public VisualStudioVersion Version { get; set; }

        /// <summary>
        /// Gets or sets the name of this edition, e.g. "Pro" (for "Professional")
        /// or "Ultimate".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the display name of this edition, e.g. "Professional" or
        /// "Premium".
        /// </summary>
        public string DisplayName
        {
            get
            {
                return Name.Equals("Pro", StringComparison.InvariantCultureIgnoreCase) ? "Professional" : Name;
            }
        }

        /// <summary>
        /// Gets or sets the installation directory of this edition.
        /// </summary>
        public string InstallationDirectory { get; set; }
    }
}
