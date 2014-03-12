using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Hosting {

    internal sealed class AssemblyResolver {

        public readonly PrivateAssemblyStore PrivateAssemblies;

        public AssemblyResolver(PrivateAssemblyStore store) {
            PrivateAssemblies = store;
        }

        public Assembly ResolveApplication(string applicationHostFile) {
            throw new NotImplementedException();
        }

        public Assembly ResolveApplicationReference(ResolveEventArgs args) {
            throw new NotImplementedException();
        }
    }
}
