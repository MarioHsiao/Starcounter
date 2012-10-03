


using NUnit.Framework;
using Starcounter.Internal.Uri;
using System;
using System.Text;
using HttpStructs;
namespace Starcounter.Internal.Test {

    [TestFixture]
    class TestRoutes : RequestHandler {



        public static void Main() {

            Reset();

/*            GET("/@s/viewmodels/subs/@s", (string app, string vm) => {
                return "404 Not Found";
            });
            GET("/@s/viewmodels/@s", (string app, string vm) => {
                return "404 Not Found";
            });
            */
            GET("/players/@i", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/dashboard/@i", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/players?@s", (string fullName) => {
                Assert.AreEqual("KalleKula", fullName);
                Console.WriteLine("f=" + fullName);
                return null;
            });

            PUT("/players/@i", (int playerId) => {
                Assert.AreEqual(123, playerId);
                //                Assert.IsNotNull(request);
                Console.WriteLine("playerId: " + playerId); //+ ", request: " + request);
                return null;
            });

            POST("/transfer?@i", (int from) => {
                Assert.AreEqual(99, from);
                Console.WriteLine("From: " + from );
                return null;
            });

            POST("/deposit?@i", (int to) => {
                Assert.AreEqual(56754, to);
                Console.WriteLine("To: " + to );
                return null;
            });

            DELETE("/all", () => {
                Console.WriteLine("deleteAll");
                return null;
            });
 
 
        }


        [Test]
        public void GenerateAstTreeOverview() {

            Reset();
            RequestProcessorCompiler.Generation = 0;

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree();
            Console.WriteLine(tree.ToString());
        }

        [Test]
        public void GenerateParseTreeOverview() {

            Reset();
            RequestProcessorCompiler.Generation = 0;

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
//            umb.
//            string str;
            Console.WriteLine(umb.CreateParseTree().ToString());
        }

        [Test]
        public void GenerateParseTreeDetails() {
            Reset();
            RequestProcessorCompiler.Generation = 0;
            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            Console.WriteLine(umb.CreateParseTree().ToString(true));
        }

        [Test]
        public void GenerateRequestProcessor() {

            RequestProcessorCompiler.Generation = 0;

            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?f=KalleKula");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?f=99&t=365&x=46");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?a=56754&x=34653");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all");

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            string ns;
            var ast = umb.CreateAstTree();
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorSourceCode( ast, out ns );

            Console.WriteLine( str );

        }

        [Test]
        public void DebugPregeneratedRequestProcessor() {
            var um = new __urimatcher__.GeneratedRequestProcessor();

            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?KalleKula");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?99");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?56754");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all");

            Main();
//            var rp = MainApp.RequestProcessor;

            foreach (var x in RequestHandler.RequestProcessor.Registrations) {
                um.Register(x.Key, x.Value.CodeAsObj );
            }

            um.Invoke(new HttpRequest(h1));
            um.Invoke(new HttpRequest(h2));
            um.Invoke(new HttpRequest(h3));
            um.Invoke(new HttpRequest(h4));
            um.Invoke(new HttpRequest(h5));
            um.Invoke(new HttpRequest(h6));
            um.Invoke(new HttpRequest(h7));
        }

        [Test]
        public void TestSimpleRestHandler() {

            RequestProcessorCompiler.Generation = 0;
            Reset();

            GET("/", () => {
                Console.WriteLine("Handler 1 was called");
                return null;
            });

            GET("/products/@s", (string prodid) => {
                Console.WriteLine("Handler 2 was called with " + prodid );
                return null;
            });

            var umb = RequestHandler.UriMatcherBuilder;

            string ns;
            var pt = umb.CreateParseTree();
            var ast = umb.CreateAstTree();
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorSourceCode(ast, out ns);

            Console.WriteLine(pt.ToString(false));
//            Console.WriteLine(pt.ToString(true));
            Console.WriteLine(ast.ToString());
            Console.WriteLine(str);

            byte[] h1 = Encoding.UTF8.GetBytes("GET / ");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /products/Test ");

            var um = RequestHandler.RequestProcessor;

            um.Invoke(new HttpRequest(h1));
            um.Invoke(new HttpRequest(h2));

        }

        [Test]
        public void TestRestHandler() {

            RequestProcessorCompiler.Generation = 0;
            
            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?KalleKula");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?99");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?56754");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all");

            Main(); // Register some handlers
            var um = RequestHandler.RequestProcessor;

            um.Invoke(new HttpRequest(h1));
            um.Invoke(new HttpRequest(h2));
            um.Invoke(new HttpRequest(h3));
            um.Invoke(new HttpRequest(h4));
            um.Invoke(new HttpRequest(h5));
            um.Invoke(new HttpRequest(h6));
            um.Invoke(new HttpRequest(h7));

        }
    }

}



