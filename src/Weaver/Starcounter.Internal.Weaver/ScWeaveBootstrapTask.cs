
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.Extensibility;

namespace Starcounter.Internal.Weaver {
    
    /// <summary>
    /// Implements the weaver that injects the proper calls to allow
    /// shell bootstraping of Starcounter executables.
    /// </summary>
    public class ScWeaveBootstrapTask : Task {

        /// <summary>
        /// Executes the weaver, effectively injecting the code into
        /// the entrypoint type to support bootstraping.
        /// </summary>
        /// <returns></returns>
        public override bool Execute() {
            throw new NotImplementedException("The bootstrap weaver is not yet implemented");
        }
    }
}
