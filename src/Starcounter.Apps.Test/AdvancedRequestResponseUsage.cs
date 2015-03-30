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
    /// Testing return types for X.
    /// </summary>
    [TestFixture]
    public class ReturnTypesForX
    {
        [Test]
        public void TestReturnTypesForX1() {

            Handle.GET("/return_400", (Request req) => {

                Assert.IsTrue("/return_400" == req.Uri);

                Assert.IsTrue("localhost:8080" == req["Host"]);
                Assert.IsTrue("localhost:8080" == req.Header["Host"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                return 400;
            });

            Handle.GET("/return_200", (Request req) => {

                Assert.IsTrue("/return_200" == req.Uri);

                Assert.IsTrue("localhost:8080" == req["Host"]);
                Assert.IsTrue("localhost:8080" == req.Header["Host"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                return 200;
            });

            Response resp;
            Object obj;
            
            obj = X.GET<String>("/return_400");
            Assert.AreEqual(obj, null);

            obj = X.GET<String>("/return_200");
            Assert.AreEqual(obj, null);

            X.GET("/return_400", out resp);
            Assert.AreEqual(resp.StatusCode, 400);

            X.GET("/return_200", out resp);
            Assert.AreEqual(resp.StatusCode, 200);
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
            localNode.IsLocalNode = true;

            Handle.GET("/response10", (Request req) =>
            {
                Assert.IsTrue("/response10" == req.Uri);

                Assert.IsTrue("127.0.0.1:8080" == req["Host"]);
                Assert.IsTrue("127.0.0.1:8080" == req.Header["Host"]);
                Assert.IsTrue("127.0.0.1:8080" == req.Host);

                Response r = new Response()
                {
                    StatusCode = 404,
                    StatusDescription = "Not Found",
                    ContentType = "text/html",
                    ContentEncoding = "gzip",
                    Cookies = {
                        "reg_fb_gate=deleted; Expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=.example.com; HttpOnly",
                        "MyCookie2=456; Domain=.foo.com; Path=/",
                        "MyCookie3=789; Path=/; Expires=Wed, 13 Jan 2021 22:23:01 GMT; HttpOnly"
                    },
                    Body = "response10"
                };

                r["Allow"] = "GET, HEAD";

                return r;
            });

            Response resp = localNode.GET("/response10", null);

            Assert.IsTrue(404 == resp.StatusCode);
            Assert.IsTrue("Not Found" == resp.StatusDescription);
            Assert.IsTrue("text/html" == resp.ContentType);
            Assert.IsTrue("gzip" == resp.ContentEncoding);

            Assert.IsTrue("reg_fb_gate=deleted; Expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=.example.com; HttpOnly" == resp.Cookies[0]);
            Assert.IsTrue("MyCookie2=456; Domain=.foo.com; Path=/" == resp.Cookies[1]);
            Assert.IsTrue("MyCookie3=789; Path=/; Expires=Wed, 13 Jan 2021 22:23:01 GMT; HttpOnly" == resp.Cookies[2]);

            Assert.IsTrue(10 == resp.ContentLength);
            //Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue("response10" == resp.Body);
            Assert.IsTrue(resp["Allow"] == "GET, HEAD");

            // Modifying response.
            resp["Allow"] = "POST";
            resp["NewHeader"] = "Haha";
            resp.StatusCode = 200;
            resp.StatusDescription = "Found";
            resp.ContentType = "application/json";
            resp.ContentEncoding = "zzzip";
            resp.Cookies = new List<String>();
            resp.Cookies.Add("MyCookie=CookieValue");
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp["Allow"]);
            Assert.IsTrue("Haha" == resp["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.Cookies[0]);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("Content-Type: application/json\r\nContent-Encoding: zzzip\r\nAllow: POST\r\nNewHeader: Haha\r\nSet-Cookie: MyCookie=CookieValue\r\n" == resp.Headers);

            Handle.GET("/response11", (Request req) =>
            {
                Assert.IsTrue("/response11" == req.Uri);
                Assert.IsTrue("127.0.0.1:8080" == req.Host);

                Response r = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                };

                r["MySuperHeader"] = "Haha!";
                r["MyAnotherSuperHeader"] = "Hahaha!";

                return r;
            });

            resp = localNode.GET("/response11", null);

            Assert.IsTrue(203 == resp.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
            Assert.IsTrue(null == resp.ContentType);
            Assert.IsTrue(null == resp.ContentEncoding);
            Assert.IsTrue(0 == resp.Cookies.Count);
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
            resp.Cookies.Add("MyCookie=CookieValue");
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp["Allow"]);
            Assert.IsTrue("Haha" == resp["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.Cookies[0]);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("MySuperHeader: Haha!\r\nMyAnotherSuperHeader: Hahaha!\r\nAllow: POST\r\nNewHeader: Haha\r\nContent-Type: application/json\r\nContent-Encoding: zzzip\r\nSet-Cookie: MyCookie=CookieValue\r\n" == resp.Headers);

            Handle.GET("/response12", (Request req) =>
            {
                Assert.IsTrue("/response12" == req.Uri);
                Assert.IsTrue("127.0.0.1:8080" == req.Host);

                return new Response()
                {
                    StatusCode = 204,
                    StatusDescription = "No Content",
                };
            });

            resp = localNode.GET("/response12", null);

            Assert.IsTrue(204 == resp.StatusCode);
            Assert.IsTrue("No Content" == resp.StatusDescription);

            Handle.GET("/response13", () =>
            {
                return new Response()
                {
                    StatusCode = 201,
                    StatusDescription = "OK"
                };
            });

            resp = localNode.GET("/response13", null);

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
            localNode.IsLocalNode = true;

            Handle.POST("/response1", (Request req) =>
            {
                Assert.IsTrue("/response1" == req.Uri);
                Assert.IsTrue("Another body!" == req.Body);
                Assert.IsTrue("MyHeader1: value1\r\nMyHeader2: value2\r\n" == req.Headers);
                Assert.IsTrue("value1" == req["MyHeader1"]);
                Assert.IsTrue("value2" == req["MyHeader2"]);
                Assert.IsTrue("127.0.0.1:8080" == req.Host);

                req["MyHeader3"] = "value3";
                req["MyHeader4"] = "value4";
                req.Method = "POST"; // TODO: Fix that GET is not default.
                req.Uri = "/response2";
                req.ConstructFromFields();

                Response resp = localNode.CustomRESTRequest(req);

                Assert.IsTrue("Haha!" == resp["MySuperHeader"]);
                Assert.IsTrue("Hahaha!" == resp["MyAnotherSuperHeader"]);
                Assert.IsTrue(203 == resp.StatusCode);
                Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
                Assert.IsTrue("Here is my body!" == resp.Body);
                Assert.IsTrue("SuperCookie=SuperValue!" == resp.Cookies[0]);
                Assert.IsTrue("text/html" == resp.ContentEncoding);

                return resp;
            });

            Handle.POST("/response2", (Request req) =>
            {
                Assert.IsTrue("/response2" == req.Uri);
                Assert.IsTrue("127.0.0.1:8080" == req.Host);

                Response resp = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                    Body = "Here is my body!"
                };

                resp["MySuperHeader"] = "Haha!";
                resp["MyAnotherSuperHeader"] = "Hahaha!";
                resp.Cookies.Add("SuperCookie=SuperValue!");
                resp.ContentEncoding = "text/html";

                return resp;
            });

            Dictionary<String, String> headers = new Dictionary<String, String> {
                { "MyHeader1", "value1" },
                { "MyHeader2", "value2" }
            };
            Response resp2 = localNode.POST("/response1", "Another body!", headers);

            Assert.IsTrue("Haha!" == resp2["MySuperHeader"]);
            Assert.IsTrue("Hahaha!" == resp2["MyAnotherSuperHeader"]);
            Assert.IsTrue(203 == resp2.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp2.StatusDescription);
       }
    }
}