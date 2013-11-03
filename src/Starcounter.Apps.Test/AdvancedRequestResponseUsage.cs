using NUnit.Framework;
using Starcounter.Advanced;
using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Internal.Web;
using TJson = Starcounter.Templates.TObject;
using TArr = Starcounter.Templates.TArray<Starcounter.Json>;
using Starcounter.Templates;

namespace Starcounter.Internal.Test
{
    /// <summary>
    /// Used for tests initialization/shutdown.
    /// </summary>
    [SetUpFixture]
    class RequestResponseUsageTestsSetup
    {
        /// <summary>
        /// HttpStructs tests initialization.
        /// </summary>
        [SetUp]
        public void InitRequestResponseUsageTestsSetup()
        {
            Db.SetEnvironment(new DbEnvironment("TestLocalNode", false));

            Dictionary<UInt16, StaticWebServer> fileServer = new Dictionary<UInt16, StaticWebServer>();
            AppRestServer appServer = new AppRestServer(fileServer);

            UserHandlerCodegen.Setup(null, null, appServer.HandleRequest);
            Node.InjectHostedImpl(UserHandlerCodegen.DoLocalNodeRest, null);
        }
    }

    /// <summary>
    /// Tests user HTTP delegates registration and usage with custom responses.
    /// </summary>
    [TestFixture]
    public class RequestResponseUsage
    {
        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestResettingFields()
        {
            // Node that is used for tests.
            Node localNode = new Node("127.0.0.1", 8080);
            localNode.LocalNode = true;

            Handle.GET("/response1", () =>
            {
                Response r = new Response()
                {
                    StatusCode = 404,
                    StatusDescription = "Not Found",
                    ContentType = "text/html",
                    ContentEncoding = "gzip",
                    SetCookie = "MyCookie1=123; MyCookie2=456",
                    Body = "response1"
                };

                r["Allow"] = "GET, HEAD";

                return r;
            });

            Response resp = localNode.GET("/response1", null);

            Assert.IsTrue(404 == resp.StatusCode);
            Assert.IsTrue("Not Found" == resp.StatusDescription);
            Assert.IsTrue("text/html" == resp.ContentType);
            Assert.IsTrue("gzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie1=123; MyCookie2=456" == resp.SetCookie);
            Assert.IsTrue(9 == resp.ContentLength);
            //Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue("response1" == resp.Body);
            Assert.IsTrue(resp["Allow"] == "GET, HEAD");

            // Modifying response.
            resp["Allow"] = "POST";
            resp["NewHeader"] = "Haha";
            resp.StatusCode = 200;
            resp.StatusDescription = "Found";
            resp.ContentType = "application/json";
            resp.ContentEncoding = "zzzip";
            resp.SetCookie = "MyCookie=CookieValue";
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp["Allow"]);
            Assert.IsTrue("Haha" == resp["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.SetCookie);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("Content-Type: application/json\r\nContent-Encoding: zzzip\r\nSet-Cookie: MyCookie=CookieValue\r\nAllow: POST\r\nNewHeader: Haha\r\n" == resp.Headers);

            Handle.GET("/response2", (Request req) =>
            {
                Response r = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                };

                r["MySuperHeader"] = "Haha!";
                r["MyAnotherSuperHeader"] = "Hahaha!";

                return r;
            });

            resp = localNode.GET("/response2", null);

            Assert.IsTrue(203 == resp.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
            Assert.IsTrue(null == resp.ContentType);
            Assert.IsTrue(null == resp.ContentEncoding);
            Assert.IsTrue(null == resp.SetCookie);
            Assert.IsTrue(0 == resp.ContentLength);
            //Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue(null == resp.Body);
            Assert.IsTrue(resp["MySuperHeader"] == "Haha!");
            Assert.IsTrue(resp["MyAnotherSuperHeader"] == "Hahaha!");
            Assert.IsTrue("MySuperHeader: Haha!\r\nMyAnotherSuperHeader: Hahaha!\r\n" == resp.Headers);

            resp["Allow"] = "POST";
            resp["NewHeader"] = "Haha";
            resp["MySuperHeader"] = "Haha!";
            resp["MyAnotherSuperHeader"] = "Hahaha!";
            resp.StatusCode = 200;
            resp.StatusDescription = "Found";
            resp.ContentType = "application/json";
            resp.ContentEncoding = "zzzip";
            resp.SetCookie = "MyCookie=CookieValue";
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp["Allow"]);
            Assert.IsTrue("Haha" == resp["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.SetCookie);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("MySuperHeader: Haha!\r\nMyAnotherSuperHeader: Hahaha!\r\nAllow: POST\r\nNewHeader: Haha\r\nContent-Type: application/json\r\nContent-Encoding: zzzip\r\nSet-Cookie: MyCookie=CookieValue\r\n" == resp.Headers);

            Handle.GET("/response3", () =>
            {
                return new Response()
                {
                    StatusCode = 204,
                    StatusDescription = "No Content",
                };
            });

            resp = localNode.GET("/response3", null);

            Assert.IsTrue(204 == resp.StatusCode);
            Assert.IsTrue("No Content" == resp.StatusDescription);

            Handle.GET("/response4", () =>
            {
                return new Response()
                {
                    StatusCode = 201,
                    StatusDescription = "OK"
                };
            });

            resp = localNode.GET("/response4", null);

            Assert.IsTrue(201 == resp.StatusCode);
            Assert.IsTrue("OK" == resp.StatusDescription);
        }

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestChangingRest()
        {
            // Node that is used for tests.
            Node localNode = new Node("127.0.0.1", 8080);
            localNode.LocalNode = true;

            Handle.POST("/response1", (Request req) =>
            {
                Assert.IsTrue("/response1" == req.Uri);
                Assert.IsTrue("Another body!" == req.Body);
                Assert.IsTrue("Host: 127.0.0.1\r\nContent-Length: 13\r\nMyHeader1: value1\r\nMyHeader2: value2\r\n" == req.Headers);
                Assert.IsTrue("value1" == req["MyHeader1"]);
                Assert.IsTrue("value2" == req["MyHeader2"]);

                req["MyHeader3"] = "value3";
                req["MyHeader4"] = "value4";
                req.Uri = "/response2";

                Response r = localNode.CustomRESTRequest(req);

                Assert.IsTrue("Haha!" == r["MySuperHeader"]);
                Assert.IsTrue("Hahaha!" == r["MyAnotherSuperHeader"]);
                Assert.IsTrue(203 == r.StatusCode);
                Assert.IsTrue("Non-Authoritative Information" == r.StatusDescription);
                Assert.IsTrue("Here is my body!" == r.Body);
                Assert.IsTrue("SuperCookie=SuperValue!" == r.SetCookie);
                Assert.IsTrue("text/html" == r.ContentEncoding);

                return r;
            });

            Handle.POST("/response2", (Request req) =>
            {
                Response r = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                    Body = "Here is my body!"
                };

                r["MySuperHeader"] = "Haha!";
                r["MyAnotherSuperHeader"] = "Hahaha!";
                r.SetCookie = "SuperCookie=SuperValue!";
                r.ContentEncoding = "text/html";

                return r;
            });

            Response resp = localNode.POST("/response1", "Another body!", "MyHeader1: value1\r\nMyHeader2: value2\r\n");

            Assert.IsTrue("Haha!" == resp["MySuperHeader"]);
            Assert.IsTrue("Hahaha!" == resp["MyAnotherSuperHeader"]);
            Assert.IsTrue(203 == resp.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
       }
    }
}