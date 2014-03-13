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

            var matches = MatchesByName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, applicationHostFile, matches);
            if (resolved != null) {
                // This is kind of an awkward case. We should either log it,
                // or figure out if we need to prevent it. We must do testing
                // with this case before we know our options for sure.
                // TODO:

                return resolved;
            }

            return Load(name, applicationHostFile);
        }

        public Assembly ResolveApplicationReference(ResolveEventArgs args) {
            var name = new AssemblyName(args.Name);

            // Always check first if we can find one loaded that has a signature
            // we consider a match. If we do, return that one.

            var matches = MatchesByName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, null, matches);
            if (resolved != null) {
                return resolved;
            }

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

        IEnumerable<Assembly> MatchesByName(Assembly[] assemblies, AssemblyName name) {
            return assemblies.Where((candidate) => {
                return candidate.GetName().Name == name.Name;
            });
        }

        Assembly MatchOne(AssemblyName name, string applicationHostFile, IEnumerable<Assembly> assemblies) {
            return assemblies.FirstOrDefault((candidate) => {
                return MatchByIdentity(candidate.GetName(), name);
            });
        }

        bool MatchByIdentity(AssemblyName first, AssemblyName second) {
            var match = first.Version.Major == second.Version.Major;
            if (match) {
                var key1 = first.GetPublicKey();
                var key2 = second.GetPublicKey();

                match = key1.Length == key2.Length;
                if (match) {
                    for (int i = 0; i < key1.Length; i++) {
                        if (key1[i] != key2[i]) {
                            match = false;
                            break;
                        }
                    }
                }
            }

            return match;
        }
    }
}