using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Server {

    /// <summary>
    /// Encapsulates the services provided by the Starcounter weaver.
    /// </summary>
    internal sealed class WeaverService {
        readonly ServerEngine engine;

        /// <summary>
        /// Initializes a <see cref="WeaverService"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ServerEngine"/> under which
        /// the weaver will run.</param>
        internal WeaverService(ServerEngine engine) {
            this.engine = engine;
        }

        /// <summary>
        /// Executes setup of <see cref="WeaverService"/>.
        /// </summary>
        internal void Setup() {
            // Do static initialization, like checking we can find
            // binaries, project files, etc.
            // TODO:

            // Keep a server-global cache with assemblies that we can
            // utilize when we don't find a particular one in the runtime
            // directory given when weaving.
            // TODO:
        }

        /// <summary>
        /// Weaves an assembly and all it's references.
        /// </summary>
        /// <param name="givenAssembly">The path to the original assembly file,
        /// normally corresponding to the path of a starting App executable.
        /// </param>
        /// <param name="runtimeDirectory">The runtime directory to where the
        /// weaved result should be stored. This directory can possibly include
        /// cached (and up-to-date) assemblies weaved from previous rounds.
        /// </param>
        /// <returns>The full path to the corresponding, weaved assembly.</returns>
        internal string Weave(string givenAssembly, string runtimeDirectory) {
            throw new NotImplementedException();
        }
    }
}
