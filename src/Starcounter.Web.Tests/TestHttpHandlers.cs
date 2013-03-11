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
            GET("/{?}", (string p1) => {
                return "GET /@s";
            });
            GET("/ab", () => {
                return "GET /ab";
            });
            /*
                                    GET("/{?}", (int x) => {
                                        Console.WriteLine("Root int called with x " + x );
                                        return "GET /@i";
                                    });
                                    GET("/{?}/{?}", (string a, int x) => {
                                        Console.WriteLine("Root int called with string " + a + " and int " + x);
                                        return "GET /@s/@i";
                                    });
                                     */
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
                return "GET /";
            });

            GET("/test", () => {
                Console.WriteLine("GET /test called");
                return null;
            });

            GET("/uri-with-req", (HttpRequest r) => {
                Assert.IsNotNull(r);
                return "GET /uri-with-req";
            });

            GET("/uri-with-req/{?}", (HttpRequest r, int i) => {
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return "GET /uri-with-req/@i";
            });

            GET("/uri-with-req/{?}", (string s, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return "GET /uri-with-req/@s";
            });

            GET("/admin/apapapa/{?}", (int i, HttpRequest r) => {
                Assert.AreEqual(19, i);
                Assert.IsNotNull(r);
                return "GET /admin/apapapa/@i";
            });

            GET("/admin/{?}", (string s, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return "GET /admin/@s";
            });

            GET("/admin/{?}/{?}", (string s, int i, HttpRequest r) => {
                Assert.AreEqual("KalleKula", s);
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return "GET /admin/@s/@i";
            });

            GET("/players", () => {
                Console.WriteLine("players");
                return "GET /players";
            });

            GET("/players/{?}/abc/{?}", (int playerId, string a) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return "GET /players/@i/abc/@s";
            });

            GET("/dashboard/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                Console.WriteLine("playerId=" + playerId);
                return "GET /dashboard/@i";
            });

            GET("/players/{?}", (int id) => {
                Assert.AreEqual(123, id);
                return "GET /players/@i";
            });

            GET("/players?{?}", (string fullName) => {
                Assert.AreEqual("KalleKula", fullName);
                Console.WriteLine("f=" + fullName);
                return "GET /players?@s";
            });

            GET("/whatever/{?}/more/{?}/{?}", (string v1, int v2, string v3) => {
                Assert.AreEqual("apapapa", v1);
                Assert.AreEqual(5547, v2);
                Assert.AreEqual("KalleKula", v3);
                return "GET /whatever/@s/more/@i/@s";
            });

            GET("/ordinary", () => {
                return "GET /ordinary";
            });

            GET("/ordAnary", () => {
                return "GET /ordAnary";
            });

            GET("/aaaaa/{?}/bbbb", (int v) => {
                Assert.AreEqual(90510, v);
                return "GET /aaaaa/@i/bbbb";
            });

            GET("/whatever/{?}/xxYx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return "GET /whatever/@s/xxYx/@i";
            });

            GET("/whatever/{?}/xxZx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return "GET /whatever/@s/xxZx/@i";
            });

            GET("/whatmore/{?}/xxZx/{?}", (string v1, int v2) => {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return "GET /whatmore/@s/xxZx/@i";
            });

            GET("/test-decimal/{?}", (decimal val) => {
                Assert.AreEqual(99.123m, val);
                return "GET /test-decimal/@m";
            });

            GET("/test-double/{?}", (double val) => {
                Assert.AreEqual(99.123d, val);
                return "GET /test-double/@d";
            });

            GET("/test-bool/{?}", (bool val) => {
                Assert.AreEqual(true, val);
                return "GET /test-bool/@b";
            });

            GET("/test-datetime/{?}", (DateTime val) => {
                DateTime expected;
                DateTime.TryParse("2013-01-17", out expected);
                Assert.AreEqual(expected, val);
                return "GET /test-datatime/@t";
            });

            GET("/static{?}/{?}", (string part, string last, HttpRequest request) => {
                Assert.AreEqual("marknad", part);
                Assert.AreEqual("nyhetsbrev", last);
                Assert.IsNotNull(request);
                return "GET /static@s/@s";
            });

            PUT("/players/{?}", (int playerId) => {
                Assert.AreEqual(123, playerId);
                //                Assert.IsNotNull(request);
                Console.WriteLine("playerId: " + playerId); //+ ", request: " + request);
                return "PUT /players/@i";
            });

            POST("/transfer?{?}", (int from) => {
                Assert.AreEqual(99, from);
                Console.WriteLine("From: " + from);
                return "POST /transfer?@i";
            });

            POST("/deposit?{?}", (int to) => {
                Assert.AreEqual(56754, to);
                Console.WriteLine("To: " + to);
                return "POST /deposit?@i";
            });

            POST("/find-player?firstname={?}&lastname={?}&age={?}", (string fn, string ln, int age) => {
                Assert.AreEqual("Kalle", fn);
                Assert.AreEqual("Kula", ln);
                Assert.AreEqual(19, age);
                return "POST /find-player?firstname=@s&lastname=@s&age=@i";
            });

            DELETE("/all", () => {
                Console.WriteLine("deleteAll");
                return "DELETE /all";
            });

        }

        [Test]
        public void GenerateSimpleCsAstTreeOverview() {

            Reset();

            RegisterSimpleHandlers(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree(false);
            tree.Namespace = "__simple_urimatcher__";
            Console.WriteLine(tree.ToString());
        }


        [Test]
        public void GenerateSimpleCppAstTreeOverview() {

            Reset();
            RegisterSimpleHandlers();
//            Main(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
            var tree = umb.CreateAstTree(true);
            tree.Namespace = "__simple_urimatcher__";
            Console.WriteLine(tree.ToString());
        }

        /// <summary>
        /// Generates the parse tree overview.
        /// </summary>
        [Test]
        public void GenerateSimpleParseTreeOverview() {

            Reset();

            RegisterSimpleHandlers(); // Register some handlers
            var umb = RequestHandler.UriMatcherBuilder;
//            umb.
//            string str;
            Console.WriteLine(umb.CreateParseTree().ToString());
        }

        /// <summary>
        /// Generates the parse tree details.
        /// </summary>
        [Test]
        public void GenerateSimpleParseTreeDetails() {
            Reset();
            RegisterSimpleHandlers(); // Register some handlers
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
            ast.Namespace = "__big_urimatcher__";
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
            ast.Namespace = "__big_urimatcher__";
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
            ast.Namespace = "__simple_urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCppSourceCode(ast);

         //   Assert.AreEqual(facit, str);

        //    Console.WriteLine("Complete codegenerated C/C++ file");
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
            ast.Namespace = "__simple_urimatcher__";
            var compiler = umb.CreateCompiler();
            var str = compiler.GenerateRequestProcessorCSharpSourceCode(ast);

            Console.WriteLine(str);

        }

        /// <summary>
        /// Generates the request processor in the C++ language.
        /// </summary>
        [Test]
        public void GenerateBigCsRequestProcessor() {
           Main(); // Register some handlers
           var umb = RequestHandler.UriMatcherBuilder;

           var ast = umb.CreateAstTree(false);
           ast.Namespace = "__big_urimatcher__";
           var compiler = umb.CreateCompiler();
           var str = compiler.GenerateRequestProcessorCSharpSourceCode(ast);

           Console.WriteLine(str);

        }

        /// <summary>
        /// Debugs the pregenerated request processor.
        /// </summary>
        [Test]
        public void DebugBigPregeneratedRequestProcessor() {
            var um = new __big_urimatcher__.GeneratedRequestProcessor();

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
            var req = new HttpRequest(h2);
            um.Invoke(req, out resource);
            Assert.AreEqual(resource,"GET /dashboard/@i");

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
        /// Debugs the pregenerated request processor.
        /// </summary>
        [Test]
        public void DebugSimplePregeneratedRequestProcessor() {
           var um = new __simple_urimatcher__.GeneratedRequestProcessor();

           byte[] h1 = Encoding.UTF8.GetBytes("GET /\r\n\r\n");
           byte[] h2 = Encoding.UTF8.GetBytes("GET /test/123\r\n\r\n");

           RegisterSimpleHandlers();

           foreach (var x in RequestHandler.RequestProcessor.Registrations) {
              um.Register(x.Key, x.Value.CodeAsObj);
           }

           object resource;

           // Test succesful URI's
           var req = new HttpRequest(h1);
           um.Invoke(req, out resource);
           Assert.AreEqual("GET /",resource);

           req = new HttpRequest(h2);
           um.Invoke(req, out resource);
           Assert.AreEqual("GET /@s/@i", resource);

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
                return "GET /";
            });

            GET("/products/{?}", (string prodid) => {
                Console.WriteLine("Handler 2 was called with " + prodid);
                return "GET /products/@s";
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
           um.Invoke(new HttpRequest(h1), out resource);
           Assert.AreEqual("GET /", resource);
           um.Invoke(new HttpRequest(h2), out resource);
           Assert.AreEqual("GET /products/@s", resource);
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
           byte[] h12 = Encoding.UTF8.GetBytes("PUT /players/123\r\n\r\n");
           byte[] h13 = Encoding.UTF8.GetBytes("GET /\r\n\r\n");

           Main(); // Register some handlers
           var um = RequestHandler.RequestProcessor;

           object resource;

           um.Invoke(new HttpRequest(h13), out resource);
           Assert.AreEqual("GET /",resource);

           um.Invoke(new HttpRequest(h1), out resource);
           Assert.AreEqual("GET /players/@i", resource);

           um.Invoke(new HttpRequest(h2), out resource);
           Assert.AreEqual("GET /dashboard/@i", resource);

            Assert.True(um.Invoke(new HttpRequest(h3), out resource));
            Assert.True(um.Invoke(new HttpRequest(h4), out resource));
            Assert.True(um.Invoke(new HttpRequest(h5), out resource));
            Assert.True(um.Invoke(new HttpRequest(h6), out resource));
            Assert.True(um.Invoke(new HttpRequest(h7), out resource));
            Assert.True(um.Invoke(new HttpRequest(h9), out resource));
            Assert.False(um.Invoke(new HttpRequest(h10), out resource));
            Assert.True(um.Invoke(new HttpRequest(h12), out resource));
            Assert.False(um.Invoke(new HttpRequest(h11), out resource), "PUT /players/123/ should not match a handler (there is a trailing slash)");
            Assert.False(um.Invoke(new HttpRequest(h8), out resource), "There is no handler DELETE /allanballan. How could it be called.");

        }


        [Test]
        public static void TestAssemblyCache() {
            Main();

            var umb = RequestHandler.UriMatcherBuilder;

            Console.WriteLine("Assembly signature:" + umb.HandlerSetChecksum);

        }

    }

}



