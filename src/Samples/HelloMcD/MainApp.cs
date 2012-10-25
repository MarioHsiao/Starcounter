using System;
using System.IO;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Application;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;

partial class MainApp : App {
    static void Main(String[] args) {
        Bootstrap();

        GET("/order", () => {
//            AssureSampleData();

            OrderApp order = new OrderApp();
            order.OrderNo = 666;
            order.View = "index.html";
            OrderApp.ItemsApp item = order.Items.Add();
//            item.Product = new OrderApp.OrderItemProductApp();

            return order;
        });

        GET("/empty", () => {
            return "empty";
        });
    }

    private static void AssureSampleData() {
        Product p = SQL("SELECT p from Product p").First;
        if (p == null) {
            Transaction(() => {
                p = new Product();
                p.Description = "Big Mc";
                p.Price = 39;
                p.ProductId = "123";

                p = new Product();
                p.Description = "QP Cheese";
                p.Price = 39;
                p.ProductId = "124";
            });
        }
    }

    #region Apps Bootstrapper
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
        fileserv.UserAddedLocalFileDirectoryWithStaticContent(Path.GetDirectoryName(typeof(MainApp).Assembly.Location) + "\\..\\.." );
        _appServer = new HttpAppServer(fileserv, new SessionDictionary());

        InternalHandlers.Register();

        App.UriMatcherBuilder.RegistrationListeners.Add((string verbAndUri) =>
        {
            UInt16 handlerId;
            GatewayHandlers.RegisterUriHandler(8080, "GET /", OnHttpMessageRoot, out handlerId);
            GatewayHandlers.RegisterUriHandler(8080, "PATCH /", OnHttpMessageRoot, out handlerId);
        });
    }

    private static Boolean OnHttpMessageRoot(HttpRequest p)
    {
        HttpResponse result = _appServer.Handle(p);
        p.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length);
        return true;
    }
    #endregion
}
