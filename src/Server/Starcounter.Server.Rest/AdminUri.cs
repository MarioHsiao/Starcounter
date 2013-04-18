
using System.Diagnostics;

namespace Starcounter.Server.Rest {
    /// <summary>
    /// Provides information about known URI's used by Starcounter tools
    /// and exposed to the community as a means to program admin servers
    /// using REST.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All URI's should be relative to the admin server base URI and any
    /// possible context path. That is, a full URI such as
    ///     http://www.myadminserver.com:8181/api/databases/foo
    /// should be expressed here as
    ///     "/databases/{?}"
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
    public static class AdminUri {
        /// <summary>
        /// Provides the context path used for admin server resources that
        /// support the programmatic REST API of admin servers.
        /// </summary>
        public const string ContextPath = "/api";

        /// <summary>
        /// Represents the resource that is the full collection of running
        /// or loaded executables in a given database. This resource will
        /// allow to be POSTed to to start an executable, the DELETE verb
        /// to allow an executable to be stopped, GET to return a list of
        /// all running executables.
        /// </summary>
        public const string HostedDatabaseExecutables = "/databases/{?}/executables";

        /// <summary>
        /// Gets the full relative admin server resource URI, including
        /// its context path.
        /// </summary>
        /// <param name="resourceUri">The resource whose URI to get.</param>
        /// <returns>A URI for the given resource, including context.</returns>
        public static string Full(string resourceUri) {
            Trace.Assert(!string.IsNullOrEmpty(resourceUri) && resourceUri.StartsWith("/"));
            return ContextPath + resourceUri;
        }
    }
}