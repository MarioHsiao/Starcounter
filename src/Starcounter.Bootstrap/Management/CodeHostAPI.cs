
namespace Starcounter.Bootstrap.Management {
    /// <summary>
    /// Defines the management API of the code host, mainly
    /// providing its URIs.
    /// </summary>
    public static class CodeHostAPI {
        static ManagementService managementService;

        /// <summary>
        /// Provides the set of code host URIs offered by the
        /// current instance.
        /// </summary>
        internal static ResourceUris Uris { get; private set; }

        /// <summary>
        /// Performs setup of the <see cref="CodeHostAPI"/>.
        /// </summary>
        /// <param name="manager"></param>
        internal static void Setup(ManagementService manager) {
            managementService = manager;
            Uris = new ResourceUris(manager.HostIdentity);
        }

        /// <summary>
        /// Initiates shutdown of the current code host.
        /// </summary>
        public static void Shutdown() {
            managementService.Shutdown();
        }

        public static ResourceUris CreateServiceURIs(string hostIdentity) {
            return new ResourceUris(hostIdentity);
        }

        /// <summary>
        /// Provides the URI's used for code host management.
        /// </summary>
        public sealed class ResourceUris {
            /// <summary>
            /// Defines the relative URI of the root context for
            /// the code host.
            /// </summary>
            public const string RootContext = "/codehost";

            /// <summary>
            /// Gets the context path in use. Assinged in the constructor.
            /// </summary>
            public readonly string ContextPath;

            /// <summary>
            /// Initializes a <see cref="ResourceUris"/>.
            /// </summary>
            /// <param name="hostIdentity">
            /// The identity of the host, normally in the form of a database
            /// name.</param>
            internal ResourceUris(string hostIdentity) {
                ContextPath = RootContext + "/" + hostIdentity.ToLowerInvariant();
            }

            /// <summary>
            /// Gets the URI of the host resource.
            /// </summary>
            public string Host {
                get {
                    return ContextPath;
                }
            }

            /// <summary>
            /// Gets the URI of the host executables resource.
            /// </summary>
            public string Executables {
                get {
                    return ContextPath + "/executables";
                }
            }
        }
    }
}
