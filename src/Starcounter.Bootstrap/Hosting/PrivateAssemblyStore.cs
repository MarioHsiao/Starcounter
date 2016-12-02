
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Starcounter.Hosting {
    /// <summary>
    /// Code-host specific adapter of a private assembly store, supporting immutable
    /// consistent views of the store at different generations to be retrived using
    /// the <c>Immutable</c> property.
    /// </summary>
    internal sealed class PrivateAssemblyStore {
        /// <summary>
        /// Provide a consistent view of the code-host global private assembly store
        /// by means of only using immutable state.
        /// </summary>
        private class ImmutableState : IPrivateAssemblyStore {
            static StringComparer comparer = StringComparer.CurrentCultureIgnoreCase;
            readonly List<string> applicationDirectories;
            readonly Dictionary<string, PrivateBinaryFile> fileToIdentity;

            public static ImmutableState Empty = new ImmutableState() {};

            private ImmutableState() {
                applicationDirectories = new List<string>();
                fileToIdentity = new Dictionary<string, PrivateBinaryFile>(ImmutableState.comparer);
            }

            public ImmutableState(ImmutableState previous, ApplicationDirectory dir) {
                applicationDirectories = new List<string>(previous.applicationDirectories.Count + 1);
                fileToIdentity = new Dictionary<string, PrivateBinaryFile>(previous.fileToIdentity.Count + dir.Binaries.Length, ImmutableState.comparer);

                foreach (var appDir in previous.applicationDirectories) {
                    applicationDirectories.Add(appDir);
                }

                foreach (var file in previous.fileToIdentity) {
                    fileToIdentity.Add(file.Key, file.Value);
                }

                applicationDirectories.Add(dir.Path);
                foreach (var binary in dir.Binaries) {
                    fileToIdentity.Add(binary.FilePath, binary);
                }
            }

            PrivateBinaryFile[] IPrivateAssemblyStore.GetAssemblies(string assemblyName) {
                var assemblies = fileToIdentity.Values.Where((candidate) => {
                    return candidate.IsAssembly && candidate.Name.Name == assemblyName;
                });
                return assemblies.OrderBy(file => file.Resolved).ToArray();
            }

            AssemblyName IPrivateAssemblyStore.GetAssembly(string filePath) {
                var record = fileToIdentity[filePath];
                if (!record.IsAssembly) throw new BadImageFormatException();
                return record.Name;
            }

            bool IPrivateAssemblyStore.IsApplicationDirectory(string applicationDirectory) {
                return applicationDirectories.FirstOrDefault((candidate) => {
                    return DirectoryExtensions.EqualDirectories(candidate, applicationDirectory);
                }) != null;
            }
        }

        private volatile ImmutableState state;
        /// <summary>
        /// Gets a snapshot of an immutable state, providing a consistent view
        /// of the assembly store.
        /// </summary>
        public IPrivateAssemblyStore Immutable {
            get {
                return state;
            }
        }

        /// <summary>
        /// Creates the code-host singleton, empty store.
        /// </summary>
        public PrivateAssemblyStore() {
            state = ImmutableState.Empty;
        }

        /// <summary>
        /// Register a new application within the store, effectively transitioning
        /// the underlying immutable state.
        /// </summary>
        /// <param name="dir">The application directory</param>
        public void RegisterApplicationDirectory(ApplicationDirectory dir) {
            var next = new ImmutableState(state, dir);
            state = next;
        }
    }
}
