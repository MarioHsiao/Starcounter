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
    /// Testing border integer values.
    /// </summary>
    [TestFixture]
    public class TestBorderIntegerValues {
        [Test]
        public void RunTest() {

            Handle.GET("/min/int64/{?}", (Request req, Int64 value) => {

                Assert.IsTrue(value == Int64.MinValue);
                
                return 200;
            });

            Handle.GET("/max/int64/{?}", (Request req, Int64 value) => {

                Assert.IsTrue(value == Int64.MaxValue);

                return 200;
            });

            Handle.GET("/min/uint64/{?}", (Request req, UInt64 value) => {

                Assert.IsTrue(value == UInt64.MinValue);

                return 200;
            });

            Handle.GET("/max/uint64/{?}", (Request req, UInt64 value) => {

                Assert.IsTrue(value == UInt64.MaxValue);

                return 200;
            });

            Handle.GET("/min/int32/{?}", (Request req, Int32 value) => {

                Assert.IsTrue(value == Int32.MinValue);

                return 200;
            });

            Handle.GET("/max/int32/{?}", (Request req, Int32 value) => {

                Assert.IsTrue(value == Int32.MaxValue);

                return 200;
            });

            Handle.GET("/min/uint32/{?}", (Request req, UInt32 value) => {

                Assert.IsTrue(value == UInt32.MinValue);

                return 200;
            });

            Handle.GET("/max/uint32/{?}", (Request req, UInt32 value) => {

                Assert.IsTrue(value == UInt32.MaxValue);

                return 200;
            });

            Response resp;

            resp = Self.GET("/min/int64/" + Int64.MinValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/max/int64/" + Int64.MaxValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/min/uint64/" + UInt64.MinValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/max/uint64/" + UInt64.MaxValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/min/int32/" + Int32.MinValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/max/int32/" + Int32.MaxValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/min/uint32/" + UInt32.MinValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);

            resp = Self.GET("/max/uint32/" + UInt32.MaxValue);
            Assert.AreEqual(resp.IsSuccessStatusCode, true);
        }
    }

    /// <summary>
    /// Testing return types for internal REST calls.
    /// </summary>
    [TestFixture]
    public class ReturnTypes
    {
        [Test]
        public void TestReturnTypesForX1() {

            Handle.GET("/return_400", (Request req) => {

                Assert.IsTrue("/return_400" == req.Uri);

                Assert.IsTrue("localhost:8080" == req.Headers["Host"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                return 400;
            });

            Handle.GET("/return_200", (Request req) => {

                Assert.IsTrue("/return_200" == req.Uri);

                Assert.IsTrue("localhost:8080" == req.Headers["Host"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                return 200;
            });

            Response resp;
            Object obj;
            
            obj = Self.GET<String>("/return_400");
            Assert.AreEqual(obj, null);

            obj = Self.GET<String>("/return_200");
            Assert.AreEqual(obj, null);

            resp = Self.GET("/return_400");
            Assert.AreEqual(resp.StatusCode, 400);

            resp = Self.GET("/return_200");
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
        /// Testing usage of headers dictionary
        /// </summary>
        [Test]
        public void TestHeadersDictionary() {

            String headers = "HeaderWithValue: haha\r\nHeaderWithoutValue: \r\n";
            Dictionary<String, String> dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("haha" == dict["HeaderWithValue"]);
            Assert.IsTrue("" == dict["HeaderWithoutValue"]);

            headers = "Header1: hahaha\r\nHeader2: haha\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("hahaha" == dict["Header1"]);
            Assert.IsTrue("haha" == dict["Header2"]);

            headers = "Header1:  hahaha\r\nHeader2:  haha\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("hahaha" == dict["Header1"]);
            Assert.IsTrue("haha" == dict["Header2"]);

            headers = "Header1: hahaha \r\nHeader2: haha \r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("hahaha " == dict["Header1"]);
            Assert.IsTrue("haha " == dict["Header2"]);

            headers = "Header1: ha ha ha\r\nHeader2:    ha ha\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("ha ha ha" == dict["Header1"]);
            Assert.IsTrue("ha ha" == dict["Header2"]);

            headers = "Header1: ha ha ha\r\nHeader2:\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("ha ha ha" == dict["Header1"]);
            Assert.IsTrue("" == dict["Header2"]);

            headers = "Header1:\r\nHeader2:\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("" == dict["Header1"]);
            Assert.IsTrue("" == dict["Header2"]);

            headers = "Header1: \r\nHeader2: \r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("" == dict["Header1"]);
            Assert.IsTrue("" == dict["Header2"]);

            headers = "Header1: abc\r\nHeader2: def\r\nHeader3: \r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("abc" == dict["Header1"]);
            Assert.IsTrue("def" == dict["Header2"]);
            Assert.IsTrue("" == dict["Header3"]);

            headers = "Header1: abc\r\nHeader2:  \r\nHeader3: f\r\n";
            dict = Response.CreateHeadersDictionaryFromHeadersString(headers);

            Assert.IsTrue("abc" == dict["Header1"]);
            Assert.IsTrue("" == dict["Header2"]);
            Assert.IsTrue("f" == dict["Header3"]);
        }

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestResettingFields()
        {
            Handle.GET("/response10", (Request req) =>
            {
                Assert.IsTrue("/response10" == req.Uri);

                Assert.IsTrue("localhost:8080" == req.Headers["Host"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                Response r = new Response()
                {
                    StatusCode = 299,
                    StatusDescription = "My Status Code",
                    ContentType = "text/html",
                    ContentEncoding = "gzip",
                    Cookies = {
                        "reg_fb_gate=deleted; Expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=.example.com; HttpOnly",
                        "MyCookie2=456; Domain=.foo.com; Path=/",
                        "MyCookie3=789; Path=/; Expires=Wed, 13 Jan 2021 22:23:01 GMT; HttpOnly"
                    },
                    Body = "response10"
                };

                r.Headers["Allow"] = "GET, HEAD";

                return r;
            });

            Response resp = Self.GET("/response10");

            Assert.IsTrue(299 == resp.StatusCode);
            Assert.IsTrue("My Status Code" == resp.StatusDescription);
            Assert.IsTrue("text/html" == resp.ContentType);
            Assert.IsTrue("gzip" == resp.ContentEncoding);

            Assert.IsTrue("reg_fb_gate=deleted; Expires=Thu, 01-Jan-1970 00:00:01 GMT; Path=/; Domain=.example.com; HttpOnly" == resp.Cookies[0]);
            Assert.IsTrue("MyCookie2=456; Domain=.foo.com; Path=/" == resp.Cookies[1]);
            Assert.IsTrue("MyCookie3=789; Path=/; Expires=Wed, 13 Jan 2021 22:23:01 GMT; HttpOnly" == resp.Cookies[2]);

            Assert.IsTrue(10 == resp.ContentLength);
            //Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue("response10" == resp.Body);
            Assert.IsTrue(resp.Headers["Allow"] == "GET, HEAD");

            // Modifying response.
            resp.Headers["Allow"] = "POST";
            resp.Headers["NewHeader"] = "Haha";
            resp.StatusCode = 200;
            resp.StatusDescription = "Found";
            resp.ContentType = "application/json";
            resp.ContentEncoding = "zzzip";
            resp.Cookies = new List<String>();
            resp.Cookies.Add("MyCookie=CookieValue");
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp.Headers["Allow"]);
            Assert.IsTrue("Haha" == resp.Headers["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.Cookies[0]);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("Content-Type: application/json\r\nContent-Encoding: zzzip\r\nAllow: POST\r\nNewHeader: Haha\r\nSet-Cookie: MyCookie=CookieValue\r\n" == resp.GetAllHeaders());

            Assert.IsTrue("application/json" == resp.Headers["Content-Type"]);
            Assert.IsTrue("POST" == resp.Headers["Allow"]);
            Assert.IsTrue("zzzip" == resp.Headers["Content-Encoding"]);
            Assert.IsTrue("Haha" == resp.Headers["NewHeader"]);

            Handle.GET("/response11", (Request req) =>
            {
                Assert.IsTrue("/response11" == req.Uri);
                Assert.IsTrue("localhost:8080" == req.Host);

                Response r = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                };

                r.Headers["MySuperHeader"] = "Haha!";
                r.Headers["MyAnotherSuperHeader"] = "Hahaha!";

                return r;
            });

            resp = Self.GET("/response11");

            Assert.IsTrue(203 == resp.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
            Assert.IsTrue(null == resp.ContentType);
            Assert.IsTrue(null == resp.ContentEncoding);
            Assert.IsTrue(0 == resp.Cookies.Count);
            Assert.IsTrue(0 == resp.ContentLength);
            //Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue(null == resp.Body);
            Assert.IsTrue(resp.Headers["MySuperHeader"] == "Haha!");
            Assert.IsTrue(resp.Headers["MyAnotherSuperHeader"] == "Hahaha!");
            Assert.IsTrue("MySuperHeader: Haha!\r\nMyAnotherSuperHeader: Hahaha!\r\n" == resp.GetAllHeaders());
            Assert.IsTrue("Haha!" == resp.Headers["MySuperHeader"]);
            Assert.IsTrue("Hahaha!" == resp.Headers["MyAnotherSuperHeader"]);

            resp.Headers["Allow"] = "POST";
            resp.Headers["NewHeader"] = "Haha";
            resp.Headers["MySuperHeader"] = "Haha!";
            resp.Headers["MyAnotherSuperHeader"] = "Hahaha!";
            resp.StatusCode = 200;
            resp.StatusDescription = "Found";
            resp.ContentType = "application/json";
            resp.ContentEncoding = "zzzip";
            resp.Cookies.Add("MyCookie=CookieValue");
            resp.Body = "Here is my body!";

            Assert.IsTrue("POST" == resp.Headers["Allow"]);
            Assert.IsTrue("Haha" == resp.Headers["NewHeader"]);
            Assert.IsTrue(200 == resp.StatusCode);
            Assert.IsTrue("Found" == resp.StatusDescription);
            Assert.IsTrue("application/json" == resp.ContentType);
            Assert.IsTrue("zzzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie=CookieValue" == resp.Cookies[0]);
            Assert.IsTrue("Here is my body!" == resp.Body);
            Assert.IsTrue("Here is my body!".Length == resp.ContentLength);
            Assert.IsTrue("MySuperHeader: Haha!\r\nMyAnotherSuperHeader: Hahaha!\r\nAllow: POST\r\nNewHeader: Haha\r\nContent-Type: application/json\r\nContent-Encoding: zzzip\r\nSet-Cookie: MyCookie=CookieValue\r\n" == resp.GetAllHeaders());
            Assert.IsTrue("Haha!" == resp.Headers["MySuperHeader"]);
            Assert.IsTrue("Hahaha!" == resp.Headers["MyAnotherSuperHeader"]);
            Assert.IsTrue("POST" == resp.Headers["Allow"]);
            Assert.IsTrue("Haha" == resp.Headers["NewHeader"]);
            Assert.IsTrue("application/json" == resp.Headers["Content-Type"]);
            Assert.IsTrue("zzzip" == resp.Headers["Content-Encoding"]);

            Handle.GET("/response12", (Request req) =>
            {
                Assert.IsTrue("/response12" == req.Uri);
                Assert.IsTrue("localhost:8080" == req.Host);

                return new Response()
                {
                    StatusCode = 204,
                    StatusDescription = "No Content",
                };
            });

            resp = Self.GET("/response12");

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

            resp = Self.GET("/response13");

            Assert.IsTrue(201 == resp.StatusCode);
            Assert.IsTrue("OK" == resp.StatusDescription);
        }

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestChangingRest()
        {
            Handle.POST("/response1", (Request req) =>
            {
                Assert.IsTrue("/response1" == req.Uri);
                Assert.IsTrue("Another body!" == req.Body);
                Assert.IsTrue("MyHeader1: value1\r\nMyHeader2: value2\r\n" == req.GetAllHeaders());
                Assert.IsTrue("value2" == req.Headers["MyHeader2"]);
                Assert.IsTrue("localhost:8080" == req.Host);

                req.Headers["MyHeader3"] = "value3";
                req.Headers["MyHeader4"] = "value4";
                req.Method = "POST"; // TODO: Fix that GET is not default.
                req.Uri = "/response2";
                req.ConstructFromFields();

                Response resp = Self.CustomRESTRequest(req);

                Assert.IsTrue("Haha!" == resp.Headers["MySuperHeader"]);
                Assert.IsTrue("Hahaha!" == resp.Headers["MyAnotherSuperHeader"]);
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
                Assert.IsTrue("localhost:8080" == req.Host);

                Response resp = new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                    Body = "Here is my body!"
                };

                resp.Headers["MySuperHeader"] = "Haha!";
                resp.Headers["MyAnotherSuperHeader"] = "Hahaha!";
                resp.Cookies.Add("SuperCookie=SuperValue!");
                resp.ContentEncoding = "text/html";

                return resp;
            });

            Dictionary<String, String> headers = new Dictionary<String, String> {
                { "MyHeader1", "value1" },
                { "MyHeader2", "value2" }
            };

            Response resp2 = Self.POST("/response1", "Another body!", null, headers);

            Assert.IsTrue("Haha!" == resp2.Headers["MySuperHeader"]);
            Assert.IsTrue("Hahaha!" == resp2.Headers["MyAnotherSuperHeader"]);
            Assert.IsTrue(203 == resp2.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp2.StatusDescription);
       }
    }
}