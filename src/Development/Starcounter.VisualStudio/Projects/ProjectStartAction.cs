using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.VisualStudio.Projects {
    /// <summary>
    /// Defines the well-known start actions possible to specify for a
    /// project exe- or library project.
    /// </summary>
    internal static class ProjectStartAction {
        internal const string Project = "Project";
        internal const string ExternalProgram = "Program";
        internal const string URL = "URL";
    }
}