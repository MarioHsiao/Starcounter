using System;
using System.IO;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Application;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;

namespace StarcounterAdministrator
{
    internal class Bootstrapper
    {
        /// <summary>
        /// The following code registers a default handler in the gateway and handles 
        /// incoming requests and dispatch them to Apps. 
        /// Also registers internal handlers for jsonpatch.
        /// 
        /// All this should be done internally in Starcounter.
        /// </summary>
        private static HttpAppServer _appServer;

        internal static void Bootstrap()
        {
            var fileserv = new StaticWebServer();
            fileserv.UserAddedLocalFileDirectoryWithStaticContent(Path.GetDirectoryName(typeof(ScAdmin).Assembly.Location));
            _appServer = new HttpAppServer(fileserv, new SessionDictionary());

            InternalHandlers.Register();

            App.UriMatcherBuilder.RegistrationListeners.Add((string verbAndUri) =>
            {
                UInt16 handlerId;
                GatewayHandlers.RegisterUriHandler(80, "GET /", HTTP_METHODS.GET_METHOD, OnHttpMessageRoot, out handlerId);
                GatewayHandlers.RegisterUriHandler(80, "PATCH /", HTTP_METHODS.PATCH_METHOD, OnHttpMessageRoot, out handlerId);
            });
        }

        /// <summary>
        /// Handles incoming messages from the gateway and dispatches them to 
        /// the correct uri-handler in Apps.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private static Boolean OnHttpMessageRoot(HttpRequest p)
        {
            HttpResponse result = _appServer.Handle(p);
            p.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length);
            return true;
        }
    }
}
