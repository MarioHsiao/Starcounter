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
using Starcounter.Advanced;
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
            HttpRequest.sc_init_http_parser();
        }
    }

    /// <summary>
    /// Class TestRoutes
    /// </summary>
    [TestFixture]
    class TestRoutes : RequestHandler {

        public static void RegisterSimpleHandlers() {
            Reset();
            GET("/", () => {
                Console.WriteLine("Root called");
                return null;
            });
            GET("/{?}", (int x) => {
                Console.WriteLine("Root int called with x " + x );
                return null;
            });
            GET("/{?}/{?}", (string a, int x) => {
                Console.WriteLine("Root int called with string " + a + " and int " + x);
                return null;
            });
        }

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
                Console.WriteLine("GET / called");
                return null;
            });

            GET("/test", () => {
                Console.WriteLine("GET /test called");
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

            GET("/admin/apapapa/{?}", (int i, HttpRequest r) => {
                Assert.AreEqual(19, i);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/admin/{?}", (string s, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/admin/{?}/{?}", (string s, int i, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return null;
            });

            GET("/players", () => {
                Console.WriteLine("players");
                return null;
            });

            GET("/players/{?}/abc/{?}", (int playerId, string a) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/dashboard/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return null;
            });

            GET("/players/{?}", (int id) => {
                Assert.AreEqual(123, id);
                return null;
            });

            GET("/players?{?}", (string fullName) => {
                Assert.AreEqual("KalleKula", fullName);
                Console.WriteLine("f=" + fullName);
                return null;
            });

            GET("/whatever/{?}/more/{?}/{?}", (string v1, int v2, string v3) => {
                Assert.AreEqual("apapapa", v1);
                Assert.AreEqual(5547, v2);
                Assert.AreEqual("KalleKula", v3);
                return null;
            });

            GET("/ordinary", () => {
                return null;
            });

            GET("/ordAnary", () => {
                return null;
            });

            GET("/aaaaa/{?}/bbbb", (int v) => {
                Assert.AreEqual(90510, v);
                return null;
            });

            GET("/whatever/{?}/xxYx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return null;
            });

            GET("/whatever/{?}/xxZx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return null;
            });

            GET("/whatmore/{?}/xxZx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return null;
            });

            GET("/test-decimal/{?}", (decimal val) => {
                Assert.AreEqual(99.123m, val);
                return null;
            });

            GET("/test-double/{?}", (double val) => {
                Assert.AreEqual(99.123d, val);
                return null;
            });

            GET("/test-bool/{?}", (bool val) => {
                Assert.AreEqual(true, val);
                return null;
            });

            GET("/test-datetime/{?}", (DateTime val) => {
                DateTime expected;
                DateTime.TryParse("2013-01-17", out expected);
                Assert.AreEqual(expected, val);
                return null;
            });

            GET("/static{?}/{?}", (string part, string last, HttpRequest request) => {
                Assert.AreEqual("marknad", part);
                Assert.AreEqual("nyhetsbrev", last);
                Assert.IsNotNull(request);
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
                Console.WriteLine("From: " + from);
                return null;
            });

            POST("/deposit?{?}", (int to) => {
                Assert.AreEqual(56754, to);
                Console.WriteLine("To: " + to);
                return null;
            });

            POST("/find-player?firstname={?}&lastname={?}&age={?}", (string fn, string ln, int age) => {
                Assert.AreEqual("Kalle", fn);
                Assert.AreEqual("Kula", ln);
                Assert.AreEqual(19, age);
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

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree(false);
            tree.Namespace = "__urimatcher__";
            Console.WriteLine(tree.ToString());
        }


        [Test]
        public void GenerateAstTreeOverviewWithInline() {

            Reset();

            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree(true);
            tree.Namespace = "__urimatcher__";
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
        /// Generates the request processor in the C# language.
        /// </summary>
        [Test]
        public void GenerateCSharpRequestProcessor() {
            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            var ast = umb.CreateAstTree(false);
            ast.Namespace = "__urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCSharpSourceCode( ast );

            Console.WriteLine( str );

        }


        /// <summary>
        /// Generates the request processor in the C++ language.
        /// </summary>
        [Test]
        public void GenerateCppRequestProcessor() {


            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            var ast = umb.CreateAstTree(true);
            ast.Namespace = "__urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCppSourceCode(ast);

            Console.WriteLine(str);
        }


        /// <summary>
        /// Generates the request processor in the C++ language.
        /// </summary>
        [Test]
        public void GenerateSimpleCppRequestProcessor() {

            var file = new System.IO.StreamReader("facit.cpp.txt");
            var facit = file.ReadToEnd();
            file.Close();



            RegisterSimpleHandlers(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            var ast = umb.CreateAstTree(true);
            ast.Namespace = "__urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCppSourceCode(ast);

//            Assert.AreEqual(facit, str);

            Console.WriteLine("Complete codegenerated C/C++ file");
            Console.WriteLine(str);



        }


        /// <summary>
        /// Generates the request processor in the C++ language.
        /// </summary>
        [Test]
        public void GenerateSimpleCsRequestProcessor() {
            RegisterSimpleHandlers(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;

            var ast = umb.CreateAstTree(false);
            ast.Namespace = "__urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCSharpSourceCode(ast);

            Console.WriteLine(str);

        }

        /// <summary>
        /// Debugs the pregenerated request processor.
        /// </summary>
        [Test]
        public void DebugPregeneratedRequestProcessor() {
            var um = new __urimatcher__.GeneratedRequestProcessor();

            // URI's that should succeed
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
            byte[] h14 = Encoding.UTF8.GetBytes("GET /admin/KalleKula/123\r\n\r\n");
            byte[] h15 = Encoding.UTF8.GetBytes("GET /admin/apapapa/19\r\n\r\n");
            byte[] h16 = Encoding.UTF8.GetBytes("GET /whatever/abrakadabra/xxYx/911\r\n\r\n");
            byte[] h17 = Encoding.UTF8.GetBytes("GET /whatever/abrakadabra/xxZx/911\r\n\r\n");
            byte[] h18 = Encoding.UTF8.GetBytes("GET /ordAnary\r\n\r\n");
            byte[] h19 = Encoding.UTF8.GetBytes("GET /ordinary\r\n\r\n");
            byte[] h20 = Encoding.UTF8.GetBytes("GET /ordinary\r\n\r\n");
            byte[] h21 = Encoding.UTF8.GetBytes("GET /whatever/apapapa/more/5547/KalleKula\r\n\r\n");
            byte[] h22 = Encoding.UTF8.GetBytes("POST /find-player?firstname=Kalle&lastname=Kula&age=19\r\n\r\n");
            byte[] h23 = Encoding.UTF8.GetBytes("GET /aaaaa/90510/bbbb\r\n\r\n");
            byte[] h24 = Encoding.UTF8.GetBytes("GET /test-decimal/99.123\r\n\r\n");
            byte[] h25 = Encoding.UTF8.GetBytes("GET /test-double/99.123\r\n\r\n");
            byte[] h26 = Encoding.UTF8.GetBytes("GET /test-bool/true\r\n\r\n");
            byte[] h27 = Encoding.UTF8.GetBytes("GET /test-datetime/2013-01-17\r\n\r\n");
            byte[] h28 = Encoding.UTF8.GetBytes("GET /staticmarknad/nyhetsbrev\r\n\r\n");
            
            // URI's that should fail due to verification or parse error.
            byte[] h501 = Encoding.UTF8.GetBytes("GET /whatever/abrakadabra/xaYx/911\r\n\r\n");     // xaYx -> xxYx
            byte[] h502 = Encoding.UTF8.GetBytes("PUT /plaiers/123\r\n\r\n");  // plaiers -> players
            byte[] h503 = Encoding.UTF8.GetBytes("GET /aaaaa/90510/DDDD\r\n\r\n"); // DDDD -> bbbb
            byte[] h504 = Encoding.UTF8.GetBytes("GET /test-decimal/DDDD\r\n\r\n"); // DDDD -> Not a decimal value
            byte[] h505 = Encoding.UTF8.GetBytes("GET /test-double/DDDD\r\n\r\n"); // DDDD -> Not a double value
            byte[] h506 = Encoding.UTF8.GetBytes("GET /test-datetime/DDDD\r\n\r\n"); // DDDD -> Not a datetime value
            byte[] h507 = Encoding.UTF8.GetBytes("GET /test-bool/DDDD\r\n\r\n"); // DDDD -> Not a boolean value
            byte[] h508 = Encoding.UTF8.GetBytes("DELETE /allanballan/DDDD\r\n\r\n"); // DDDD -> Not a boolean value

            Main();

            foreach (var x in RequestHandler.RequestProcessor.Registrations) {
                um.Register(x.Key, x.Value.CodeAsObj );
            }

            object resource;

            // Test succesful URI's
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
            Assert.True(um.Invoke(new HttpRequest(h12), out resource));
            Assert.True(um.Invoke(new HttpRequest(h13), out resource));
            Assert.True(um.Invoke(new HttpRequest(h14), out resource));
            Assert.True(um.Invoke(new HttpRequest(h15), out resource));
            Assert.True(um.Invoke(new HttpRequest(h16), out resource));
            Assert.True(um.Invoke(new HttpRequest(h17), out resource));
            Assert.True(um.Invoke(new HttpRequest(h18), out resource));
            Assert.True(um.Invoke(new HttpRequest(h19), out resource));
            Assert.True(um.Invoke(new HttpRequest(h20), out resource));
            Assert.True(um.Invoke(new HttpRequest(h21), out resource));
            Assert.True(um.Invoke(new HttpRequest(h22), out resource));
            Assert.True(um.Invoke(new HttpRequest(h23), out resource));
            Assert.True(um.Invoke(new HttpRequest(h24), out resource));
            Assert.True(um.Invoke(new HttpRequest(h25), out resource));
            Assert.True(um.Invoke(new HttpRequest(h26), out resource));
            Assert.True(um.Invoke(new HttpRequest(h27), out resource));
            Assert.True(um.Invoke(new HttpRequest(h28), out resource));

            // Test URI's that should fail.
            Assert.False(um.Invoke(new HttpRequest(h501), out resource));
            Assert.False(um.Invoke(new HttpRequest(h502), out resource));
            Assert.False(um.Invoke(new HttpRequest(h503), out resource));
            Assert.False(um.Invoke(new HttpRequest(h504), out resource));
            Assert.False(um.Invoke(new HttpRequest(h505), out resource));
            Assert.False(um.Invoke(new HttpRequest(h506), out resource));
            Assert.False(um.Invoke(new HttpRequest(h507), out resource));
            Assert.False(um.Invoke(new HttpRequest(h508), out resource), "There is no handler DELETE /allanballan. How could it be found?");

        }

        /// <summary>
        /// </summary>
        [Test]
        public void TestSimpleUriConflict() {

            Reset();

            GET("/ab", () => {
                Console.WriteLine("Handler /ab was called" );
                return 2;
            });

            GET("/{?}", (string rest) => {
                Console.WriteLine("Handler / was called");
                return 1;
            });

            var umb = RequestHandler.UriMatcherBuilder;

            var pt = umb.CreateParseTree();
            var ast = umb.CreateAstTree(false);
            ast.Namespace = "__urimatcher__";
            
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCSharpSourceCode(ast);

            Console.WriteLine(pt.ToString(false));
//            Console.WriteLine(pt.ToString(true));
            Console.WriteLine(ast.ToString());
            Console.WriteLine(str);

            byte[] h1 = Encoding.UTF8.GetBytes("GET /\r\n\r\n");
            byte[] h2 = Encoding.UTF8.GetBytes("GET /ab\r\n\r\n");

            var um = RequestHandler.RequestProcessor;
            object resource;
            Assert.True(um.Invoke(new HttpRequest(h1), out resource));
            Assert.AreEqual(1, (int)resource);
            Assert.True(um.Invoke(new HttpRequest(h2), out resource));
            Assert.AreEqual(2, (int)resource);
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
                Console.WriteLine("Handler 2 was called with " + prodid);
                return null;
            });

            var umb = RequestHandler.UriMatcherBuilder;

            var pt = umb.CreateParseTree();
            var ast = umb.CreateAstTree(false);
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCSharpSourceCode(ast);

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
            byte[] h8 = Encoding.UTF8.GetBytes("DELETE /allanballan\r\n\r\n");
            byte[] h9 = Encoding.UTF8.GetBytes("GET /test\r\n\r\n");
            byte[] h10 = Encoding.UTF8.GetBytes("GET /testing\r\n\r\n");
            byte[] h11 = Encoding.UTF8.GetBytes("PUT /players/123/\r\n\r\n");
            byte[] h12 = Encoding.UTF8.GetBytes("PUT /players/123 ");
            byte[] h13 = Encoding.UTF8.GetBytes("PUT /players/123\n");

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
            Assert.False(um.Invoke(new HttpRequest(h8), out resource), "There is no handler DELETE /allanballan. How could it be called.");
            Assert.True(um.Invoke(new HttpRequest(h9), out resource));
            Assert.False(um.Invoke(new HttpRequest(h10), out resource));
            Assert.False(um.Invoke(new HttpRequest(h11), out resource));
            Assert.True(um.Invoke(new HttpRequest(h11), out resource));
            Assert.True(um.Invoke(new HttpRequest(h11), out resource));

        }


        [Test]
        public static void TestAssemblyCache() {
            Main();

            var umb = RequestHandler.UriMatcherBuilder;

            Console.WriteLine("Assembly signature:" + umb.HandlerSetChecksum);

        }

    }

}



