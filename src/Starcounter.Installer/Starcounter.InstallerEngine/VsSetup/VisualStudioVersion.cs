using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.InstallerEngine.VsSetup
{
    /// <summary>
    /// Repesent a well-known Visual Studio version, i.e. "Visual Studio 2010".
    /// </summary>
    public class VisualStudioVersion
    {
        /// <summary>
        /// Represents the Visual Studio version 2010 ("10.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2010 = new VisualStudioVersion("2010", "10.0");

        /// <summary>
        /// Represents the Visual Studio version 2012 ("11.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2012 = new VisualStudioVersion("2012", "11.0");

        /// <summary>
        /// Represents the Visual Studio version 2013 ("12.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2013 = new VisualStudioVersion("2013", "12.0");

        /// <summary>
        /// Represents the Visual Studio version 2015 ("14.0").
        /// </summary>
        public static readonly VisualStudioVersion VS2015 = new VisualStudioVersion("2015", "14.0");


        /// <summary>
        /// Initializes a <see cref="VisualStudioVersion"/>.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="number"></param>
        private VisualStudioVersion(string year, string number)
        {
            this.Year = year;
            this.BuildNumber = number;
        }

        /// <summary>
        /// Gets the year of this Visual Studio version, e.g "2010".
        /// </summary>
        public readonly string Year;

        /// <summary>
        /// Gets the build number of this Visual Studio version, e.g "10.0".
        /// </summary>
        public readonly string BuildNumber;
    }
}
