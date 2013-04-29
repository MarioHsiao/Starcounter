
using System;
using System.Diagnostics;

namespace Starcounter.Server.Rest {
    /// <summary>
    /// Provides functionality usable when programming against
    /// the admin server REST API. The principal service offered
    /// is the resolving of URIs, through <see cref="AdminAPI.Uris"/>.
    /// </summary>
    public sealed class AdminAPI {
        /// <summary>
        /// Provides the set of admin server URIs offered by the
        /// current instance.
        /// </summary>
        public readonly ResourceUris Uris;

        /// <summary>
        /// Provides information about known URI's used by Starcounter tools
        /// and exposed to the community as a means to program admin servers
        /// using REST.
        /// </summary>
        /// <remarks>
        /// <para>
        /// All URI's should be relative to the admin server base URI and any
        /// possible context path. That is, a full URI such as
        ///     http://www.myadminserver.com:8181/api/objects/foo
        /// should be expressed here as
        ///     "/objects/{?}"
        /// when being implemented in a property.
        /// </para>
        /// <para>
        /// All URI's should begin with an included forward slash.
        /// </para>
        /// <para>
        /// All URI's should be defined with the same syntax we use to define
        /// handlers, meaning replacement values should be in brackets with a
        /// question mark, i.e. {?}.
        /// </remarks>
        /// </para>
        public sealed class ResourceUris {
            /// <summary>
            /// Provides the default context path used for admin server resources
            /// that support the programmatic REST API of admin servers.
            /// </summary>
            public const string DefaultContextPath = "/api";

            /// <summary>
            /// Gets the context path in used. Assinged in the constructor.
            /// </summary>
            public readonly string ContextPath;

            /// <summary>
            /// Gets the URI of the root, i.e. the REST entrypoint.
            /// </summary>
            public string Root {
                get {
                    return ContextPath;
                }
            }

            /// <summary>
            /// Gets the URI of the root server resource.
            /// </summary>
            public string Server {
                get {
                    return ContextPath + "/server";
                }
            }

            /// <summary>
            /// Gets the URI of the root databases collection resource.
            /// </summary>
            public string Databases {
                get {
                    return ContextPath + "/databases";
                }
            }

            /// <summary>
            /// Gets the URI template to use to address a single
            /// database resource.
            /// </summary>
            public string Database {
                get {
                    return Databases + "/{?}";
                }
            }

            /// <summary>
            /// Gets the URI of the root database engines collection
            /// resource.
            /// </summary>
            public string Engines {
                get {
                    return ContextPath + "/engines";
                }
            }

            /// <summary>
            /// Gets the URI template to use to address a single
            /// database engine resource.
            /// </summary>
            public string Engine {
                get {
                    return Engines + "/{?}";
                }
            }

            /// <summary>
            /// Gets the URI template of a database engine executable
            /// collection resource.
            /// </summary>
            public string Executables {
                get {
                    return Engine + "/executables";
                }
            }

            /// <summary>
            /// Gets the URI template to use to address a single
            /// database engine executable resource.
            /// </summary>
            public string Executable {
                get {
                    return Executables + "/{?}";
                }
            }

            /// <summary>
            /// Initializes a new instance of <see cref="ResourceUris"/>,
            /// defining the context path to use to resolve all relative URIs.
            /// </summary>
            /// <param name="contextPath">The context path to use.</param>
            internal ResourceUris(string contextPath) {
                this.ContextPath = contextPath;
            }
        }

        /// <summary>
        /// Initializes a new instance of <see cref="AdminAPI"/>, defining
        /// the context path to use to resolve all relative URIs.
        /// </summary>
        /// <param name="contextPath">The context path to use.</param>
        public AdminAPI(string contextPath = ResourceUris.DefaultContextPath) {
            this.Uris = new ResourceUris(contextPath);
        }

        /// <summary>
        /// Formats the given URI and inserts arguments into it.
        /// </summary>
        /// <param name="uri">The URI to format</param>
        /// <param name="args">The arguments to insert instead of
        /// URI template placeholders.</param>
        /// <returns>A string with all template placeholders
        /// replaced with values from <paramref name="args"/>.</returns>
        public string FormatUri(string uri, params object[] args) {
            for (int i = 0; args != null && i < args.Length; i++) {
                int index = uri.IndexOf("{?}");
                if (index == -1) throw new ArgumentOutOfRangeException("args");
                uri = uri.Remove(index, 3);
                uri = uri.Insert(index, args[i].ToString());
            }
            if (uri.Contains("{?}"))
                throw new ArgumentOutOfRangeException("args");

            return uri;
        }
    }
}