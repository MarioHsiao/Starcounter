using System;
using System.IO;
using System.Reflection;
using System.Text;
using HttpStructs;
using Starcounter;
using Starcounter.Internal.Uri;
using Starcounter.Internal.Web;

namespace HelloGateway
{
    class Program : App
    {
        static void Main(string[] args)
        {
            Bootstrap();

            GET("/users", () => "Hello Users!");
            GET("/hello/@s", ( string s ) => "Hello " + s );
            //GET("/myapp", () => new Master() { View="master.html"} );
        }
 
        private static Boolean OnHttpMessageRoot(HttpRequest p)
        {
            HttpResponse result = AppServer.Handle(p);
            p.WriteResponse(result.Uncompressed, 0, result.Uncompressed.Length); 
            return true;
        }

        public static HttpAppServer AppServer;

        public static void Bootstrap()
        {
            var fileserv = new StaticWebServer();
            fileserv.UserAddedLocalFileDirectoryWithStaticContent(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            AppServer = new HttpAppServer(fileserv, null);

            App.UriMatcherBuilder.RegistrationListeners.Add( (string verbAndUri) => {
                UInt16 handlerId;
                GatewayHandlers.RegisterUriHandler(80, "GET /", HTTP_METHODS.GET_METHOD, OnHttpMessageRoot, out handlerId);
            });
        }
    }
}
