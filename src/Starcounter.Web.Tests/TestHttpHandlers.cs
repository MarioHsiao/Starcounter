// ***********************************************************************
// <copyright file="TestHttpHandlers.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using NUnit.Framework;
using Starcounter.Internal.Uri;
using System;
using System.Text;
using HttpStructs;
namespace Starcounter.Internal.Test {

    /// <summary>
    /// Used for HttpStructs tests initialization/shutdown.
    /// </summary>
    [SetUpFixture]
    public class HttpStructsTestsSetup
    {
        /// <summary>
        /// HttpStructs tests initialization.
        /// </summary>
        [SetUp]
        public void InitHttpStructsTests()
        {
            HttpStructs.HttpRequest.sc_init_http_parser();
        }
    }

    /// <summary>
    /// Class TestRoutes
    /// </summary>
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

            GET("/", () => {
                Console.WriteLine("Root called");
                return null;
            });

            GET("/uri-with-req", (HttpRequest r) => {
                Assert.IsNotNull(r);
                return null;
            });

            GET("/uri-with-req/{?}", (HttpRequest r, int i) => {
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/uri-with-req/{?}", (string s, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/admin/{?}", (string s, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/admin/apapapa/{?}", (int i, HttpRequest r) => {
                //                Assert.AreEqual("KalleKula", s);
                //                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/admin/{?}/{?}", (string s, int i, HttpRequest r) => {
//                Assert.AreEqual("KalleKula", s);
//                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return null;
            });

            //GET("/admin/{?}/{?}", (string s, int i, HttpRequest r) => {
            //    Assert.AreEqual("KalleKula", s);
            //    Assert.IsNotNull(r);
            //    return null;
            //});

            GET("/players", () => {
                Console.WriteLine("players");
                return null;
            });

            GET("/players/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/dashboard/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/players?{?}", (string fullName) => {
                Assert.AreEqual("KalleKula", fullName);
                Console.WriteLine("f=" + fullName);
                return null;
            });

            PUT("/players/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                //                Assert.IsNotNull(request);
                Console.WriteLine("playerId: " + playerId); //+ ", request: " + request);
                return null;
            });

            POST("/transfer?{?}", (int from) => {
                Assert.AreEqual(99, from);
                Console.WriteLine("From: " + from );
                return null;
            });

            POST("/deposit?{?}", (int to) => {
                Assert.AreEqual(56754, to);
                Console.WriteLine("To: " + to );
                return null;
            });

            //POST("/find-player?firstname={?}&lastname={?}&age={?}", (string fn, string ln, int age) => {

            //    return null;
            //});

            DELETE("/all", () => {
                Console.WriteLine("deleteAll");
                return null;
            });
        }

        private static void SetupComplexTemplates() {
            
            GET("/{?}/viewmodels/{?}", (string app, string vm) => {
                return "404 Not Found";
            });

            POST("/find-player?firstname={?}&lastname={?}&age={?}", (string fn, string ln, int age) => {
                return null;
            });
        }

        [Test]
        public void GenerateAstTreeOverview() {

            Reset();

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree();
            Console.WriteLine(tree.ToString());
        }

        /// <summary>
        /// Generates the ast tree overview.
        /// </summary>
        [Test]
        public void GenerateComplexAstTreeOverview() {
            Reset();

            SetupComplexTemplates();
            
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree();
            Console.WriteLine(tree.ToString());
        }

        /// <summary>
        /// Generates the parse tree overview.
        /// </summary>
        [Test]
        public void GenerateParseTreeOverview() {

            Reset();

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
//            umb.
//            string str;
            Console.WriteLine(umb.CreateParseTree().ToString());
        }

        /// <summary>
        /// Generates the parse tree details.
        /// </summary>
        [Test]
        public void GenerateParseTreeDetails() {
            Reset();
            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            Console.WriteLine(umb.CreateParseTree().ToString(true));
        }

        /// <summary>
        /// Generates the request processor.
        /// </summary>
        [Test]
        public void GenerateRequestProcessor() {

            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123\r\n\r\n");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123\r\n\r\n");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?f=KalleKula\r\n\r\n");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123\r\n\r\n");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?f=99&t=365&x=46\r\n\r\n");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?a=56754&x=34653\r\n\r\n");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all");

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            var ast = umb.CreateAstTree();
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorSourceCode( ast );

            Console.WriteLine( str );

        }

        /// <summary>
        /// Debugs the pregenerated request processor.
        /// </summary>
        [Test]
        public void DebugPregeneratedRequestProcessor() {
            var um = new __urimatcher__.GeneratedRequestProcessor();

            // TODO: 
            // Add test for sending in the wrong datatype in the uri.
            // The parser doesn't fail as it should.

            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123\r\n\r\n");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123\r\n\r\n");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?KalleKula\r\n\r\n");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123\r\n\r\n");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?99\r\n\r\n");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?56754\r\n\r\n");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all\r\n\r\n");
            byte[] h8 = Encoding.UTF8.GetBytes("GET /players\r\n\r\n");
            byte[] h9 = Encoding.UTF8.GetBytes("GET /\r\n\r\n");
            byte[] h10 = Encoding.UTF8.GetBytes("GET /uri-with-req\r\n\r\n");
            byte[] h11 = Encoding.UTF8.GetBytes("GET /uri-with-req/123\r\n\r\n");
            byte[] h12 = Encoding.UTF8.GetBytes("GET /uri-with-req/KalleKula\r\n\r\n");
            byte[] h13 = Encoding.UTF8.GetBytes("GET /admin/KalleKula\r\n\r\n");
            byte[] h14 = Encoding.UTF8.GetBytes("GET /admin/KalleKula/123r\n\r\n");
            byte[] h15 = Encoding.UTF8.GetBytes("GET /admin/Kalle/46544r\n\r\n");


            Main();
//            var rp = MainApp.RequestProcessor;

            foreach (var x in RequestHandler.RequestProcessor.Registrations) {
                um.Register(x.Key, x.Value.CodeAsObj );
            }

            object resource;
            Assert.True(um.Invoke(new HttpRequest(h1), out resource));
            Assert.True(um.Invoke(new HttpRequest(h2), out resource));
            Assert.True(um.Invoke(new HttpRequest(h3), out resource));
            Assert.True(um.Invoke(new HttpRequest(h4), out resource));
            Assert.True(um.Invoke(new HttpRequest(h5), out resource));
            Assert.True(um.Invoke(new HttpRequest(h6), out resource));
            Assert.True(um.Invoke(new HttpRequest(h7), out resource));
            Assert.True(um.Invoke(new HttpRequest(h8), out resource)); 
            Assert.True(um.Invoke(new HttpRequest(h9), out resource));
            Assert.True(um.Invoke(new HttpRequest(h10), out resource));
            Assert.True(um.Invoke(new HttpRequest(h11), out resource));

            // TODO:
            // The ParseInt function does not verify that the specified string
            // is numbers or not, needs to be fixed.
//            Assert.True(um.Invoke(new HttpRequest(h12), out resource));

            Assert.True(um.Invoke(new HttpRequest(h13), out resource));
            Assert.True(um.Invoke(new HttpRequest(h14), out resource));
            Assert.True(um.Invoke(new HttpRequest(h15), out resource));
        }

        /// <summary>
        /// Tests the simple rest handler.
        /// </summary>
        [Test]
        public void TestSimpleRestHandler() {

            Reset();

            GET("/", () => {
                Console.WriteLine("Handler 1 was called");
                return null;
            });

            GET("/products/{?}", (string prodid) => {
                Console.WriteLine("Handler 2 was called with " + prodid );
                return null;
            });

            var umb = RequestHandler.UriMatcherBuilder;

            var pt = umb.CreateParseTree();
            var ast = umb.CreateAstTree();
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorSourceCode(ast);

            Console.WriteLine(pt.ToString(false));
//            Console.WriteLine(pt.ToString(true));
            Console.WriteLine(ast.ToString());
            Console.WriteLine(str);

            byte[] h1 = Encoding.UTF8.GetBytes("GET /\r\n\r\n");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /products/Test\r\n\r\n");

            var um = RequestHandler.RequestProcessor;
            object resource;
            Assert.True(um.Invoke(new HttpRequest(h1), out resource));
            Assert.True(um.Invoke(new HttpRequest(h2), out resource));
        }

        /// <summary>
        /// Tests the rest handler.
        /// </summary>
        [Test]
        public void TestRestHandler() {

            byte[] h1 = Encoding.UTF8.GetBytes("GET /players/123\r\n\r\n");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /dashboard/123\r\n\r\n");
            byte[] h3 = Encoding.UTF8.GetBytes("GET /players?KalleKula\r\n\r\n");
            byte[] h4 = Encoding.UTF8.GetBytes("PUT /players/123\r\n\r\n");
            byte[] h5 = Encoding.UTF8.GetBytes("POST /transfer?99\r\n\r\n");
            byte[] h6 = Encoding.UTF8.GetBytes("POST /deposit?56754\r\n\r\n");
            byte[] h7 = Encoding.UTF8.GetBytes("DELETE /all\r\n\r\n");

            Main(); // Register some handlers
            var um = RequestHandler.RequestProcessor;

            object resource;
            Assert.True(um.Invoke(new HttpRequest(h1), out resource));
            Assert.True(um.Invoke(new HttpRequest(h2), out resource));
            Assert.True(um.Invoke(new HttpRequest(h3), out resource));
            Assert.True(um.Invoke(new HttpRequest(h4), out resource));
            Assert.True(um.Invoke(new HttpRequest(h5), out resource));
            Assert.True(um.Invoke(new HttpRequest(h6), out resource));
            Assert.True(um.Invoke(new HttpRequest(h7), out resource));

        }


        [Test]
        public static void TestAssemblyCache() {
            Main();

            var umb = RequestHandler.UriMatcherBuilder;

            Console.WriteLine("Assembly signature:" + umb.HandlerSetChecksum);

        }

    }

}



