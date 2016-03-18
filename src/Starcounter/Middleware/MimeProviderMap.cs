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

        // For internal validation / testing
        int installedProviderSets = 0;

        /// <summary>
        /// The representation we keep of any provider once it's installed (i.e
        /// when Use() is invoked).
        /// </summary>
        class Node {
            internal MimeProvisionDelegate Target;
            internal MimeProviderMap.Node Next;
            internal int Generation;

            public void Invoke(MimeProviderContext context) {
                Target(context, () => {
                    context.ProvidersInvoked++;
                    Next.Invoke(context);
                });
            }
        }

        class RootHandle {
            internal MimeType Type;
            internal Node Root;
            internal int Generation;
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
                    var currentRootHandle = mimeProviders[index];
                    rootHandle.Generation = currentRootHandle.Generation + 1;
                    node.Generation = rootHandle.Generation;

                    var tail = CloneRecursive(currentRootHandle.Root, rootHandle);
                    tail.Next = node;
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

        void Assert(bool b) {
            if (!b) throw new System.Exception();
        }

        internal void Test() {
            bool firstNullFound = false;

            for (int i = 0; i < MaxMimeProviderSets; i++) {
                var rh = mimeProviders[i];

                if (rh != null) {
                    Assert(!firstNullFound);
                    Assert(i < installedProviderSets);
                }
                else {
                    firstNullFound = true;
                    Assert(i >= installedProviderSets);
                }
            }

            for (int i = 0; i < MaxMimeProviderSets; i++) {
                var rh = mimeProviders[i];
                if (rh == null) {
                    break;
                }

                Assert(rh.Root != null);
                var node = rh.Root;
                while (node != null) {
                    if (node != terminator) {
                        Assert(node.Generation == rh.Generation);
                    }
                    else {
                        Assert(node.Next == null);
                    }

                    Assert(node.Target != null);
                    node = node.Next;
                }
            }
        }

        byte[] InvokeAll(MimeType type, Request request, IResource resource) {
            Test();

            var index = IndexOf(type);
            if (index == -1) {
                return null;
            }

            var context = new MimeProviderContext(request, resource);
            var rootHandle = mimeProviders[index];

            rootHandle.Root.Invoke(context);

            return context.Result;
        }

        Node CloneRecursive(Node node, RootHandle rh, Node prevClone = null) {
            var clone = new Node() { Target = node.Target };
            clone.Generation = rh.Generation;

            var result = clone;
            if (rh.Root == null) {
                rh.Root = clone;
            }
            if (prevClone != null) {
                prevClone.Next = clone;
            }
            if (node.Next != terminator) {
                result = CloneRecursive(node.Next, rh, clone);
            }

            return result;
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

            installedProviderSets++;
            mimeProviders[emptySlot] = rootHandle;
        }
    }
}
