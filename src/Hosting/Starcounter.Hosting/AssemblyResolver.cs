using System;
using System.Collections.Generic;
using System.IO;
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
            // Always check first if we can find one loaded that has a signature
            // we consider match. If we do, return that one.
            // TODO:

            // If we find none, or if we don't consider them a match, start
            // looking for them in our private bin store.

            var requesting = args.RequestingAssembly;
            if (requesting == null) {
                // We don't resolve references if we don't have a requesting
                // assembly. There is a likelyhood this resolver is not what
                // fits the needs, and we must tribute possible other resolvers.
                // Debug log / trace this.
                // TODO:
                return null;
            }
            else if (!PrivateAssemblies.IsApplicationDirectory(Path.GetDirectoryName(requesting.Location))) {
                // We only resolve references between assemblies stored in any
                // of the application directories.
                // Debug log / trace this.
                // TODO:
                return null;
            }

            // See if we can find an assembly with the given name. If we can't,
            // we can't resolve. If we find one, load that - and log if we do it
            // from another directory than the requestee. If we find several, we
            // need to determine which to load.
            // TODO:

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
