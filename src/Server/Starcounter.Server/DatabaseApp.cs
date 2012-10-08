using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {
    /// <summary>
    /// Represents an "App" as it relates to a <see cref="Database"/> under
    /// a given server.
    /// </summary>
    internal sealed class DatabaseApp {
        /// <summary>
        /// Gets or sets the path to the original executable, i.e. the
        /// executable that caused the materialization of this instance.
        /// </summary>
        internal string OriginalExecutablePath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the path to the executable file actually being
        /// loaded into the database, i.e. normally to the file after it
        /// has been weaved.
        /// </summary>
        internal string ExecutionPath {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the working directory of the App.
        /// </summary>
        internal string WorkingDirectory {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the full argument set passed to the executable when
        /// started, possibly including both arguments targeting Starcounter
        /// and/or the actual App Main.
        /// </summary>
        internal string[] Arguments {
            get;
            set;
        }
    }
}
