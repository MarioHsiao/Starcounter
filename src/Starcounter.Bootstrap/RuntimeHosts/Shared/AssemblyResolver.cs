using Sc.Server.Weaver.Schema;
using Starcounter.Internal;
using Starcounter.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Starcounter.Hosting {
    
    /// <summary>
    /// Implements the Starcounter Code Host Assembly resolver.
    /// The primary service offered by the assembly resolver is
    /// to locate referenced assemblies the CLR don't know how
    /// to load.
    /// </summary>
    /// <remarks>
    /// There is an article about the assembly resolver on the
    /// wiki: /wiki/How-the-Code-Host-Locates-Assemblies. Make
    /// sure to update this article of any of the resolving
    /// internals change.
    /// </remarks>
    internal sealed class AssemblyResolver : IAssemblyResolver {
        readonly LogSource log = LogSources.CodeHostAssemblyResolver;
        Assembly appAssembly;

        public readonly PrivateAssemblyStore PrivateAssemblies;

        public AssemblyResolver(PrivateAssemblyStore store) {
            Trace("Assembly resolver created in process {0}", Process.GetCurrentProcess().Id);
            PrivateAssemblies = store;
            appAssembly = null;
        }

        ApplicationDirectory IAssemblyResolver.RegisterApplication(string executablePath, out DatabaseSchema schema)
        {
            var exe = new FileInfo(executablePath);
            var appDir = new ApplicationDirectory(exe.Directory);

            PrivateAssemblies.RegisterApplicationDirectory(appDir);

            // Cheat: pre-load the application assembly.
            appAssembly = ResolveApplication(executablePath);
            if (appAssembly.EntryPoint == null)
            {
                throw ErrorCode.ToException(
                    Error.SCERRAPPLICATIONNOTANEXECUTABLE, string.Format("Failing application file: {0}", executablePath));
            }

            var stream = appAssembly.GetManifestResourceStream(DatabaseSchema.EmbeddedResourceName);
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);
                schema = DatabaseSchema.DeserializeFrom(stream);
            }
            else
            {
                // We don't require apps to include any database code to allow them
                // to run in the shared host. Just give back an empty schema to
                // denote this.
                schema = new DatabaseSchema();
                if (!StarcounterEnvironment.IsAdministratorApp)
                {
                    log.LogNotice($"Application {Path.GetFileName(executablePath)} does not define a database schema.");
                }
            }
            
            return appDir;
        }

        Assembly IAssemblyResolver.ResolveApplication(string executablePath)
        {
            Debug.Assert(appAssembly != null);
            return appAssembly;
        }
        
        Assembly IAssemblyResolver.ResolveApplicationReference(ResolveEventArgs args) {
            Trace("Asked to resolve reference to {0}, requested by {1}", args.Name, args.RequestingAssembly == null ? "<unknown>" : args.RequestingAssembly.FullName);
            
            var name = new AssemblyName(args.Name);
            var store = PrivateAssemblies.Immutable;

            // Always check first if we can find one loaded that has a signature
            // we consider a match. If we do, return that one.

            var matches = GetAllWithName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, null, matches);
            if (resolved != null) {
                Trace("Reference to {0} resolved to loaded assembly {1}:{2}", name.FullName, resolved.FullName, resolved.Location); 
                return resolved;
            }

            Trace("Could not resolve {0} to loaded assembly.", name.FullName);

            // If we find none, or if we don't consider them a match, start
            // looking for them in our private bin store.

            var requesting = args.RequestingAssembly;
            var pick =  requesting != null ? 
                ResolveApplicationReferenceScoped(name, requesting, store) : 
                ResolveApplicationReferenceUnscoped(name, store);

            return pick == null ? null : Load(pick.Name, pick.FilePath);
        }

        Assembly ResolveApplication(string applicationHostFile)
        {
            Trace("Resolving application: {0}", applicationHostFile);

            var store = PrivateAssemblies.Immutable;
            var name = store.GetAssembly(applicationHostFile);

            var matches = GetAllWithName(AppDomain.CurrentDomain.GetAssemblies(), name);
            var resolved = MatchOne(name, applicationHostFile, matches);
            if (resolved != null)
            {
                // This is kind of an awkward case. We log a notice about
                // it to help out investigating if something later behaves
                // weird. We might consider not supporting this later (i.e
                // have the application to fail starting instead).
                Trace("Application loaded: {0}, resolved to {1}{2}", applicationHostFile, resolved.FullName, resolved.Location);
                log.LogNotice(
                    "Redirecting application assembly {0}, executable {1} to already loaded {2}",
                    resolved.FullName, applicationHostFile, resolved.Location);
                return resolved;
            }

            return Load(name, applicationHostFile);
        }

        PrivateBinaryFile ResolveApplicationReferenceUnscoped(AssemblyName name, IPrivateAssemblyStore store) {
            // No requesting assembly usually means a bind failed from an
            // Assembly.Load() call, with a partial name.
            
            var candidates = store.GetAssemblies(name.Name);
            if (candidates.Length == 0) {
                Trace("Failed resolving {0}: no such assemblies found among private assemblies", name.FullName);
                return null;
            }

            var pick = MatchOne(name, candidates);
            if (pick == null) {
                Trace("Failed resolving {0}: none of the {1} found assembly files matched.", name.FullName, candidates.Length);
                return null;
            }

            return pick;
        }

        PrivateBinaryFile ResolveApplicationReferenceScoped(AssemblyName name, Assembly requestingAssembly, IPrivateAssemblyStore store) {
            var scope = requestingAssembly.Location;
            var applicationDirectory = Path.GetDirectoryName(scope);
            if (!store.IsApplicationDirectory(applicationDirectory)) {
                // We only resolve references between assemblies stored in any
                // of the application directories.
                Trace("Failed resolving {0}: requesting assembly not from a known path ({1})", name.FullName, scope);
                return null;
            }

            // See if we can find an assembly with the given name. If we can't,
            // we can't resolve. If we find one, load that - and log if we do it
            // from another directory than the requestee. If we find several, we
            // need to determine which to load.

            var candidates = store.GetAssemblies(name.Name);
            if (candidates.Length == 0) {
                Trace("Failed resolving {0}: no such assemblies found among private assemblies", name.FullName);
                return null;
            }

            var pick = MatchOne(name, candidates, applicationDirectory);
            if (pick == null) {
                Trace("Failed resolving {0}: none of the {1} found assembly files matched.", name.FullName, candidates.Length);
                return null;
            }

            return pick;
        }

        Assembly Load(AssemblyName name, string assemblyFilePath) {
            var msg = string.Format("Loading assembly {0} from {1}", name.FullName, assemblyFilePath);
            Trace(msg);
            log.Debug(msg);

            return Assembly.LoadFile(assemblyFilePath);
        }

        IEnumerable<Assembly> GetAllWithName(Assembly[] assemblies, AssemblyName name) {
            return assemblies.Where((candidate) => {
                return AssemblyName.ReferenceMatchesDefinition(candidate.GetName(), name);
            });
        }

        Assembly MatchOne(AssemblyName name, string applicationHostFile, IEnumerable<Assembly> assemblies) {
            return assemblies.FirstOrDefault((candidate) => {
                return IsCompatibleVersions(candidate.GetName(), name);
            });
        }

        PrivateBinaryFile MatchOne(AssemblyName name, PrivateBinaryFile[] alternatives, string requestingApplicationDirectory = null) {
            // The match is:
            //   1. A compatible one in the same directory as the one requsting the file.
            //   2. A semantically matching version (from any directory).
            //   3. The first other that compatible.
            //   4. None.

            PrivateBinaryFile pick = null;

            if (requestingApplicationDirectory != null) {
                pick = alternatives.FirstOrDefault((candidate) => {
                    return IsCompatibleVersions(candidate.Name, name) &&
                        candidate.IsFromApplicaionDirectory(requestingApplicationDirectory);
                });
            }

            pick = pick ?? alternatives.FirstOrDefault((candidate) => {
                return IsCompatibleVersions(candidate.Name, name, true);
            });

            pick = pick ?? alternatives.FirstOrDefault((candidate) => {
                return IsCompatibleVersions(candidate.Name, name);
            });

            return pick;
        }

        bool IsCompatibleVersions(AssemblyName first, AssemblyName second, bool includeMinor = false) {
            var match = first.Version.Major == second.Version.Major;
            if (includeMinor) match = first.Version.Minor == second.Version.Minor;
            return match;
        }

        [Conditional("TRACE")]
        void Trace(string message, params object[] args) {
            Diagnostics.WriteTrace(log.Source, 0, string.Format(message, args));
        }
    }
}