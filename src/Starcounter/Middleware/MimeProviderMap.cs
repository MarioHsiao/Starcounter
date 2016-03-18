
namespace Starcounter {
    /// <summary>
    /// Map of installed MIME providers. Governs thread-safe registration and
    /// lock-free consumption of MIME providers.
    /// </summary>
    internal sealed class MimeProviderMap {
        const int MaxMimeProviderSets = 20;
        object writeMonitor = new object();
        readonly RootHandle[] mimeProviders = new RootHandle[MaxMimeProviderSets];
        Node terminator = new Node() { Target = MimeProvider.Terminator };

        /// <summary>
        /// The representation we keep of any provider once it's installed (i.e
        /// when Use() is invoked).
        /// </summary>
        class Node {
            internal MimeProvisionDelegate Target;
            internal MimeProviderMap.Node Next;

            public void Invoke(MimeProviderContext context) {
                Target(context, () => {
                    Next.Invoke(context);
                });
            }

            public Node Clone() {
                return new Node() { Target = this.Target, Next = this.Next };
            }
        }

        class RootHandle {
            internal MimeType Type;
            internal Node Root;
        }

        /// <summary>
        /// Installs the given provisioner as a provider of the specified
        /// MIME type.
        /// </summary>
        /// <param name="type">The MIME type <paramref name="target"/> can
        /// provide.</param>
        /// <param name="target">The provisioner</param>
        public void Install(MimeType type, MimeProvisionDelegate target) {
            lock (writeMonitor) {
                var node = new Node() {
                    Target = target,
                    Next = terminator
                };
                var rootHandle = new RootHandle() { Type = type };
                
                var index = IndexOf(type);
                if (index == -1) {
                    // No previous provider. Add our root handle with the single
                    // node last in the list.
                    rootHandle.Root = node;
                    Add(rootHandle);
                }
                else {
                    // Clone the installed list. Add our node last.
                    var currentRoot = mimeProviders[index].Root;
                    var newRoot = currentRoot.Clone();
                    rootHandle.Root = newRoot;

                    var current = currentRoot;
                    var ptr = newRoot;
                    while (current.Target != terminator.Target) {
                        ptr.Next = current.Clone();

                        current = current.Next;
                        ptr = ptr.Next;
                    }

                    // We've reached the final node: it points to the terminator.
                    // Replace it with the new node we have produced.
                    ptr.Next = node;

                    // Finally, install the root handle at the allocated
                    // MIME type index.
                    mimeProviders[index] = rootHandle;
                }
            }
        }

        public static byte[] Invoke(string application, MimeType type, Request request, IResource resource) {
            if (string.IsNullOrEmpty(application)) {
                return null;
            }

            var providers = Application.GetFastNamedApplication(application).MimeProviders;
            return providers.InvokeAll(type, request, resource);
        }

        byte[] InvokeAll(MimeType type, Request request, IResource resource) {
            var index = IndexOf(type);
            if (index == -1) {
                return null;
            }

            var context = new MimeProviderContext(request, resource);
            var rootHandle = mimeProviders[index];
            rootHandle.Root.Invoke(context);

            return context.Result;
        }

        int IndexOf(MimeType type) {
            int index = -1;
            for (int i = 0; i < MaxMimeProviderSets; i++) {
                var rootHandle = mimeProviders[i];
                if (rootHandle == null) {
                    break;
                }
                else if (rootHandle.Type == type) {
                    index = i;
                    break;
                }
            }
            return index;
        }

        void Add(RootHandle rootHandle) {
            int emptySlot = -1;
            for (int i = 0; i < MaxMimeProviderSets; i++) {
                if (mimeProviders[i] == null) {
                    emptySlot = i;
                    break;
                }
            }
            if (emptySlot == -1) {
                throw ErrorCode.ToException(Error.SCERRBADARGUMENTS, 
                    string.Format("Too many variants of providers: we currently support only {0} MIME types", MaxMimeProviderSets));
            }

            mimeProviders[emptySlot] = rootHandle;
        }
    }
}
