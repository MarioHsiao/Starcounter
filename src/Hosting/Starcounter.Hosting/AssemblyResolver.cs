using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Starcounter.Hosting {

    internal sealed class AssemblyResolver {

        public readonly PrivateAssemblyStore PrivateAssemblies;

        public AssemblyResolver(PrivateAssemblyStore store) {
            PrivateAssemblies = store;
        }

        public Assembly ResolveApplication(string applicationHostFile) {
            Trace("Resolving application: {0}", applicationHostFile);

            var name = PrivateAssemblies.GetAssembly(applicationHostFile);

            var matches = MatchesByName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, applicationHostFile, matches);
            if (resolved != null) {
                // This is kind of an awkward case. We should either log it,
                // or figure out if we need to prevent it. We must do testing
                // with this case before we know our options for sure.
                // TODO:
                Trace("Application loaded: {0}, resolved to {1}{2}", applicationHostFile, resolved.FullName, resolved.Location);
                return resolved;
            }

            return Load(name, applicationHostFile);
        }

        public Assembly ResolveApplicationReference(ResolveEventArgs args) {
            Trace("Asked to resolve reference to {0}, requested by {1}", args.Name, args.RequestingAssembly == null ? "<unknown>" : args.RequestingAssembly.FullName);
            
            var name = new AssemblyName(args.Name);

            // Always check first if we can find one loaded that has a signature
            // we consider a match. If we do, return that one.

            var matches = MatchesByName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, null, matches);
            if (resolved != null) {
                Trace("Reference to {0} resolved to loaded assembly {1}:{2}", name.FullName, resolved.FullName, resolved.Location); 
                return resolved;
            }

            Trace("Could not resolve {0} to loaded assembly.", name.FullName);

            // If we find none, or if we don't consider them a match, start
            // looking for them in our private bin store.

            var requesting = args.RequestingAssembly;
            if (requesting == null) {
                // We don't resolve references if we don't have a requesting
                // assembly. There is a likelyhood this resolver is not what
                // fits the needs, and we must tribute possible other resolvers.
                Trace("Failed resolving {0}: no requesting assembly", name.FullName);
                return null;
            }
            else if (!PrivateAssemblies.IsApplicationDirectory(Path.GetDirectoryName(requesting.Location))) {
                // We only resolve references between assemblies stored in any
                // of the application directories.
                Trace("Failed resolving {0}: requesting assembly not from a known path ({1})", name.FullName, requesting.Location);
                return null;
            }

            // See if we can find an assembly with the given name. If we can't,
            // we can't resolve. If we find one, load that - and log if we do it
            // from another directory than the requestee. If we find several, we
            // need to determine which to load.

            var candidates = PrivateAssemblies.GetAssemblies(name.Name);
            if (candidates.Length == 0) {
                Trace("Failed resolving {0}: no such assemblies found among private assemblies", name.FullName);
                return null;
            }

            var pick = MatchOne(candidates, requesting);
            if (pick == null) {
                Trace("Failed resolving {0}: none of the {1} found assembly files matched.", name.FullName, candidates.Length);
                return null;
            }

            return Load(pick.Name, pick.Path);
        }

        Assembly Load(AssemblyName name, string assemblyFilePath) {
            Trace("Loading assembly {0} from {1}", name.FullName, assemblyFilePath);
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

        PrivateBinaryFile MatchOne(PrivateBinaryFile[] alternatives, Assembly requesting) {
            // The match would be something like:
            //   One in the same directory as the one requested.
            //   One exactly matching the version (from any directory).
            //   The first other.
            // TODO:
            return alternatives.Single();
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

        [Conditional("TRACE")]
        static void Trace(string message, params object[] args) {
            System.Diagnostics.Trace.WriteLine(string.Format(message, args));
        }
    }
}