using System;
using System.IO;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Application;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;

partial class Master : App {

    static void Main(String[] args) {
        
        Bootstrap();

        GET("/empty", () => {
            return new Master() { View = "master.html" };
        });

        GET("/test", () => new TestApp() );

        GET("/products/@s", (string id) => {
            var master = Get<Master>("/~master");
            master.Page = new ProductApp() {
                Data = SQL( "SELECT Product FROM Product WHERE Id=?",id ).First,
                View = "product.html"
            };
            return master;
        });
/*
        GET("/orders/{id}", (int id) => {
            var master = Get<Master>("/~master");
            master.Page = new OrderApp() {
                Data = SQL( "SELECT Order FROM Order WHERE Id=?", id ).First,
                View = "orderentry.html"
            };
            return master;
        });

        GET("/new-order", (string id) => {
            var master = Get<Master>("/~master");
            master.Page = new OrderApp() {
                Data = new Order(),
                View = "orderentry.html"
            };
            return master;
        });
 */
    }

    private static HttpAppServer _appServer;

    /// <summary>
    /// Function that registers a default handler in the gateway and handles incoming requests
    /// and dispatch them to Apps. Also registers internal handlers for jsonpatch.
    /// 
    /// All this should be done internally in Starcounter.
    /// </summary>
    private static void Bootstrap()
    {
        var fileserv = new StaticWebServer();
        fileserv.UserAddedLocalFileDirectoryWithStaticContent(Path.GetDirectoryName(typeof(Master).Assembly.Location));
        _appServer = new HttpAppServer(fileserv, new SessionDictionary());

        InternalHandlers.Register();

        App.UriMatcherBuilder.RegistrationListeners.Add((string verbAndUri) =>
        {
            UInt16 handlerId;
            GatewayHandlers.RegisterUriHandler(80, "GET /", HTTP_METHODS.GET_METHOD, OnHttpMessageRoot, out handlerId);
            GatewayHandlers.RegisterUriHandler(80, "PATCH /", HTTP_METHODS.PATCH_METHOD, OnHttpMessageRoot, out handlerId);
        });
    }

    private static Boolean OnHttpMessageRoot(HttpRequest p)
    {
        HttpResponse result = _appServer.Handle(p);
        p.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length);
        return true;
    }
}
