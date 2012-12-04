
using HttpStructs;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;
using System;
namespace Starcounter.Internal {

    /// <summary>
    /// Sets up the REST, Hypermedia and Apps modules
    /// </summary>
    /// <remarks>
    /// A common dependency injection pattern for all bootstrapping in Starcounter should be
    /// considered for a future version. This includes Apps, SQL and other modules.
    /// </remarks>
    public static class AppsBootstrapper {


        private static HttpAppServer _appServer;
        private static StaticWebServer fileServer;

        /// <summary>
        /// Adds a directory to the list of directories used by the web server to
        /// resolve GET requests for static content.
        /// </summary>
        /// <param name="path">The directory to include</param>
        public static void AddFileServingDirectory(string path) {
            fileServer.UserAddedLocalFileDirectoryWithStaticContent(path);
        }

        /// <summary>
        /// Function that registers a default handler in the gateway and handles incoming requests
        /// and dispatch them to Apps. Also registers internal handlers for jsonpatch.
        /// 
        /// All this should be done internally in Starcounter.
        /// </summary>
        /// <param name="port">Listens for http traffic on the given port. </param>
        /// <param name="resourceResolvePath">Adds a directory path to the list of paths used when resolving a request for a static REST (web) resource</param>
        public static void Bootstrap(int port = -1, string resourceResolvePath = null) {

            fileServer = new StaticWebServer();
            if (resourceResolvePath != null)
                AddFileServingDirectory(resourceResolvePath);

            _appServer = new HttpAppServer(fileServer);

            // Register the handlers required by the Apps system. These work as user code handlers, but
            // listens to the built in REST api serving view models to REST clients.
            InternalHandlers.Register();

            // Let the Network Gateway now when the user adds a handler (like GET("/")).
            App.UriMatcherBuilder.RegistrationListeners.Add((string verbAndUri) => {
                UInt16 handlerId;

                if (port != -1) {
                    // TODO! Alexey. Please allow to register to Gateway with only port (i.e without Verb and URI)
                    GatewayHandlers.RegisterUriHandler((ushort)port, "GET /", OnHttpMessageRoot, out handlerId);
                    // TODO! Alexey. Please allow to register to Gateway with only port (i.e without Verb and URI)
                    GatewayHandlers.RegisterUriHandler((ushort)port, "PATCH /", OnHttpMessageRoot, out handlerId);
                }
            });
        }

        /// <summary>
        /// Entrypoint for all incoming http requests from the Network Gateway.
        /// </summary>
        /// <param name="request">The http request</param>
        /// <returns>Returns true if the request was handled</returns>
        private static Boolean OnHttpMessageRoot(HttpRequest request) {
            HttpResponse result = _appServer.Handle(request);
            request.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length);
            return true;
        }
    }
}
