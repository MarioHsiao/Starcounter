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
            var name = PrivateAssemblies.GetAssembly(applicationHostFile);
            
            var matches = 
                AppDomain.CurrentDomain.GetAssemblies().Where((candidate) => {
                return candidate.GetName().Name == name.Name;
            });

            if (matches == null /*matches.count == 0?*/) {
                return Load(name, applicationHostFile);
            }

            var pick = PickMatch(name, applicationHostFile, matches);
            return pick ?? Load(name, applicationHostFile);
        }

        public Assembly ResolveApplicationReference(ResolveEventArgs args) {
            throw new NotImplementedException();
        }

        Assembly Load(AssemblyName name, string assemblyFilePath) {
            return Assembly.LoadFile(assemblyFilePath);
        }

        Assembly PickMatch(AssemblyName name, string applicationHostFile, IEnumerable<Assembly> assemblies) {
            // TODO:
            // Implement our matching algorithm
            return null;
        }
    }
}
