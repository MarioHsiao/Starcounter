
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {
    /// <summary>
    /// Represents the collection of private assemblies the code host is
    /// aware of, based on loaded applications.
    /// </summary>
    internal sealed class PrivateAssemblyStore {
        /// <summary>
        /// The current store.
        /// </summary>
        public static readonly PrivateAssemblyStore Current = new PrivateAssemblyStore();

        private PrivateAssemblyStore() {
        }
    }
}
