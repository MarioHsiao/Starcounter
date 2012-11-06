using System;
using System.IO;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Application;
using Starcounter.Internal.JsonPatch;
using Starcounter.Internal.Web;

namespace HelloMcd {
    partial class MainApp : App {
        //private static String RESOURCE_DIRECTORY = @"c:\Code\Level1\src\Samples\HelloMcd";    // chr
        private static String RESOURCE_DIRECTORY = @"c:\GitHub\Level1\src\Samples\HelloMcD";    // andwah
        //private static String RESOURCE_DIRECTORY = @"HelloMcd"; // Marcin

        static void Main(String[] args) {
            Bootstrap();
            AssureSampleData();
           
            GET("/order", () => {
                LongRunningTransaction.NewCurrent();
                Order order = new Order() { OrderNo = 666 };
                return new OrderApp() { Data = order, View = "index.html" };
            });

            // TODO:
            // Added to avoid a bug when only one handler is registered.
            GET("/empty", () => {
                return "empty";
            });
        }

        private static void TestIt() {
            Db.Transaction(() => {
                Order order = new Order();
                order.OrderNo = 19;

                OrderItem item = new OrderItem();
                item.Order = order;
                item.Price = 15;
                item.Quantity = 2;

                decimal total = order.Total;

                item = new OrderItem();
                item.Order = order;
                item.Price = 15;
                item.Quantity = 2;

                total = order.Total;

                foreach (OrderItem oi in order.Items) {
                    oi.Quantity = 10;
                }

                total = order.Total;
            });
        }

        private static void AssureSampleData() {
            Product p;
            StreamReader reader;
            String productStr;

            p = SQL("SELECT p from Product p").First;
            if (p == null) {
                using (reader = new StreamReader(RESOURCE_DIRECTORY + "\\Products.txt")) {
                    Db.Transaction(() => {

                        // TODO:
                        // Add prices and id to file.
                        while (!reader.EndOfStream) {
                            productStr = reader.ReadLine();

                            if (!string.IsNullOrEmpty(productStr)) {
                                p = new Product();
                                p.Description = productStr;
                                p.Price = 99;
                                p.ProductId = productStr;
                            }
                        }
                    });
                }
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
        private static void Bootstrap() {
            var fileserv = new StaticWebServer();
            //        fileserv.UserAddedLocalFileDirectoryWithStaticContent(Path.GetDirectoryName(typeof(MainApp).Assembly.Location) + "\\..\\.." );
            fileserv.UserAddedLocalFileDirectoryWithStaticContent(RESOURCE_DIRECTORY);
            _appServer = new HttpAppServer(fileserv, new SessionDictionary());

            InternalHandlers.Register();

            App.UriMatcherBuilder.RegistrationListeners.Add((string verbAndUri) => {
                UInt16 handlerId;
                GatewayHandlers.RegisterUriHandler(8080, "GET /", OnHttpMessageRoot, out handlerId);
                GatewayHandlers.RegisterUriHandler(8080, "PATCH /", OnHttpMessageRoot, out handlerId);
            });
        }

        private static Boolean OnHttpMessageRoot(HttpRequest p) {
            HttpResponse result = _appServer.Handle(p);
            p.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length);
            return true;
        }
        #endregion
    }
}