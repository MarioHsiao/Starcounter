using Starcounter.CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.CLI {
    /// <summary>
    /// Provides the principal entrypoint to use when a CLI client
    /// want to use the common way to start an executable.
    /// </summary>
    public static class Executable {
        /// <summary>
        /// Runs the executable as
        /// </summary>
        /// <param name="exePath">Full path to the executable.</param>
        /// <param name="args">Parsed arguments to use to customize the
        /// settings under which the exeuctable will run and possibly
        /// parameters to be sent to the entrypoint.</param>
        public static void Exec(string exePath, ApplicationArguments args) {
            throw new NotImplementedException();
        }
    }
}
