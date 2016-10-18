
using Starcounter.Administrator.API.Utilities;
using Starcounter.Server;
using Starcounter.Server.PublicModel;
using Starcounter.Server.Rest;
using System;

namespace Starcounter.Administrator.API.Handlers {
    /// <summary>
    /// Provides handlers for REST calls targeting the admin server API
    /// root resource (i.e. the principal API REST entrypoint).
    /// </summary>
    internal static partial class RootHandler {
        /// <summary>
        /// Provides all handler classes with a single URI set and makes
        /// sure it's only accessible from one place.
        /// </summary>
        /// <remarks>
        /// This way, we can start the admin server with a dependency
        /// injection pattern with what URI set to use for the API, using
        /// different sets in tests (like test doubles) and experiment
        /// with a new set for a newer version of the API if we like.
        /// </remarks>
        public static AdminAPI API { get; private set; }

        /// <summary>
        /// Relax "REST confirmance" by not registering 405's on all resource URI's
        /// that does not support all methods.
        /// </summary>
        public static bool Disable405Registrations { get; private set; }

        /// <summary>
        /// Provides a set of references to the currently running
        /// admin server host. Used and shared by handlers when fullfilling
        /// REST requests.
        /// </summary>
        public static class Host {
            public static ServerEngine Engine { get; private set; }
            public static IServerRuntime Runtime { get; private set; }
            public static string ServerHost { get; private set; }
            public static int ServerPort { get; private set; }
            public static Uri BaseUri { get; private set; }

            public static void Setup(
                string serverHost,
                int serverPort,
                ServerEngine engine,
                IServerRuntime runtime) {
                Engine = engine;
                Runtime = runtime;
                ServerHost = serverHost;
                ServerPort = serverPort;
                BaseUri = new UriBuilder(Uri.UriSchemeHttp, serverHost, serverPort).Uri;
            }
        }

        /// <summary>
        /// Sets up the root API handler.
        /// </summary>
        /// <param name="adminAPI">The API to be used by all
        /// fellow handlers.</param>
        /// <param name="disable405registrations">
        /// Relax "REST confirmance" by not registering 405's on all resource URI's
        /// that does not support all methods.
        /// </param>
        public static void Setup(AdminAPI adminAPI, bool disable405registrations = false) {
            API = adminAPI;
            Disable405Registrations = true;

            var uri = adminAPI.Uris.Root;
            Handle.GET(uri, () => { return 403; });
            Register405OnAllUnsupported(uri, new string[] { "GET" });
        }

        public static void Register405OnAllUnsupported(string uri, string[] methodsSupported, bool allowExtensionsBeyondPatch = false) {
            if (RootHandler.Disable405Registrations) {
                return;
            }

            RESTUtility.Register405OnAllUnsupported(uri, (ushort)Host.ServerPort, methodsSupported, allowExtensionsBeyondPatch);
        }

        /// <summary>
        /// Makes a uri or uri template absolute to the host bound to
        /// the current root handler.
        /// </summary>
        /// <param name="relativeUriTemplate">Relative uri or uri template.</param>
        /// <param name="args">Optional arguments, used if the uri is a template.</param>
        /// <returns>A final and absolute URI relating to the API root.</returns>
        public static string ToAbsoluteUri(this string relativeUriTemplate, params object[] args) {
            var relative = relativeUriTemplate;
            if (args != null || args.Length > 0) {
                relative = API.FormatUri(relativeUriTemplate, args);
            }
            return new Uri(Host.BaseUri, relative).ToString();
        }
    }
}