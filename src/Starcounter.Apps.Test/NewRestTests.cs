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

namespace Starcounter.Internal.Test
{
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
            Db.SetEnvironment(new DbEnvironment("TestLocalNode", false));

            Dictionary<UInt16, StaticWebServer> fileServer = new Dictionary<UInt16, StaticWebServer>();
            HttpAppServer appServer = new HttpAppServer(fileServer);

            UserHandlerCodegen.Setup(null, null, appServer.HandleRequest);
        }
    }

    /// <summary>
    /// Tests user HTTP delegates registration and usage with custom responses.
    /// </summary>
    [TestFixture]
    public class CustomResponseTests
    {
        /// <summary>
        /// Tests some helper functions.
        /// </summary>
        [Test]
        public void TestHelperFunctions()
        {
            Byte[] b = Encoding.ASCII.GetBytes("-1");
            Int64 r = Utf8Helper.IntFastParseFromAscii(b, 0, 2);
            Assert.IsTrue(r == -1);

            b = Encoding.ASCII.GetBytes("0");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 1);
            Assert.IsTrue(r == 0);

            b = Encoding.ASCII.GetBytes("-0");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 2);
            Assert.IsTrue(r == 0);

            b = Encoding.ASCII.GetBytes("-12345");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 6);
            Assert.IsTrue(r == -12345);

            b = Encoding.ASCII.GetBytes("12345");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 5);
            Assert.IsTrue(r == 12345);

            Byte[] t = new Byte[32];
            UInt32 num_bytes;
            String ss;
            unsafe
            {
                fixed (Byte* tt = t)
                {
                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 0);
                    Assert.IsTrue(1 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("0" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 3);
                    Assert.IsTrue(1 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("3" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 1);
                    Assert.IsTrue(1 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("1" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, -1);
                    Assert.IsTrue(2 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("-1" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, -7);
                    Assert.IsTrue(2 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("-7" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 17);
                    Assert.IsTrue(2 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("17" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, -123);
                    Assert.IsTrue(4 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("-123" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, -346456456);
                    Assert.IsTrue(10 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("-346456456" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 54645);
                    Assert.IsTrue(5 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("54645" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, 6786787798);
                    Assert.IsTrue(10 == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue("6786787798" == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, Int64.MinValue);
                    Assert.IsTrue(Int64.MinValue.ToString().Length == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue(Int64.MinValue.ToString() == ss);

                    num_bytes = Utf8Helper.WriteIntAsUtf8(tt, Int64.MaxValue);
                    Assert.IsTrue(Int64.MaxValue.ToString().Length == num_bytes);
                    ss = new String((SByte*)tt, 0, (Int32)num_bytes, Encoding.ASCII);
                    Assert.IsTrue(Int64.MaxValue.ToString() == ss);
                }
            }

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 0);
            Assert.IsTrue(1 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("0" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 1);
            Assert.IsTrue(1 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("1" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, -1);
            Assert.IsTrue(2 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("-1" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, -7);
            Assert.IsTrue(2 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("-7" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 3);
            Assert.IsTrue(1 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("3" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 17);
            Assert.IsTrue(2 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("17" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, -123);
            Assert.IsTrue(4 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("-123" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, -346456456);
            Assert.IsTrue(10 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("-346456456" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 54645);
            Assert.IsTrue(5 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("54645" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, 6786787798);
            Assert.IsTrue(10 == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue("6786787798" == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, Int64.MinValue);
            Assert.IsTrue(Int64.MinValue.ToString().Length == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue(Int64.MinValue.ToString() == ss);

            num_bytes = Utf8Helper.WriteIntAsUtf8Man(t, 0, Int64.MaxValue);
            Assert.IsTrue(Int64.MaxValue.ToString().Length == num_bytes);
            ss = UTF8Encoding.UTF8.GetString(t, 0, (Int32)num_bytes);
            Assert.IsTrue(Int64.MaxValue.ToString() == ss);

            unsafe
            {
                b = Encoding.ASCII.GetBytes("0");
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr) pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, 1, out r);
                    Assert.IsTrue(is_num && r == 0);
                }

                b = Encoding.ASCII.GetBytes("-0");
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr)pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, 2, out r);
                    Assert.IsTrue(is_num && r == 0);
                }

                b = Encoding.ASCII.GetBytes("-12345");
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr)pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, 6, out r);
                    Assert.IsTrue(is_num && r == -12345);
                }

                b = Encoding.ASCII.GetBytes("12345a");
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr)pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, 6, out r);
                    Assert.IsTrue(!is_num);
                }

                b = Encoding.ASCII.GetBytes(Int64.MinValue.ToString());
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr)pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, Int64.MinValue.ToString().Length, out r);
                    Assert.IsTrue(is_num && r == Int64.MinValue);
                }

                b = Encoding.ASCII.GetBytes(Int64.MaxValue.ToString());
                fixed (Byte* pb = b)
                {
                    IntPtr p = (IntPtr)pb;
                    Boolean is_num = Utf8Helper.IntFastParseFromAscii(p, Int64.MaxValue.ToString().Length, out r);
                    Assert.IsTrue(is_num && r == Int64.MaxValue);
                }
            }

            b = Encoding.ASCII.GetBytes("0");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 1);
            Assert.IsTrue(r == 0);

            b = Encoding.ASCII.GetBytes("-0");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 2);
            Assert.IsTrue(r == 0);

            b = Encoding.ASCII.GetBytes("-12345");
            r = Utf8Helper.IntFastParseFromAscii(b, 0, 6);
            Assert.IsTrue(r == -12345);

            b = Encoding.ASCII.GetBytes(Int64.MinValue.ToString());
            r = Utf8Helper.IntFastParseFromAscii(b, 0, Int64.MinValue.ToString().Length);
            Assert.IsTrue(r == Int64.MinValue);

            b = Encoding.ASCII.GetBytes(Int64.MaxValue.ToString());
            r = Utf8Helper.IntFastParseFromAscii(b, 0, Int64.MaxValue.ToString().Length);
            Assert.IsTrue(r == Int64.MaxValue);
        }

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestLocalNode()
        {
            // Node that is used for tests.
            Node localNode = new Node("127.0.0.1", 8080);
            localNode.InternalSetLocalNodeForUnitTests();

            Handle.GET("/response1", () =>
            {
                return new Response()
                {
                    StatusCode = 404,
                    StatusDescription = "Not Found",
                    ContentType = "text/html",
                    ContentEncoding = "gzip",
                    Headers = "Allow: GET, HEAD\r\n",
                    SetCookie = "MyCookie1=123; MyCookie2=456",
                    Body = "response1"
                };
            });

            Response resp = localNode.GET("/response1", null, null);

            resp.ResetAllCustomFields();

            Assert.IsTrue(404 == resp.StatusCode);
            Assert.IsTrue("Not Found" == resp.StatusDescription);
            Assert.IsTrue("text/html" == resp.ContentType);
            Assert.IsTrue("gzip" == resp.ContentEncoding);
            Assert.IsTrue("MyCookie1=123; MyCookie2=456" == resp.SetCookie);
            Assert.IsTrue(9 == resp.ContentLength);
            Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue("response1" == resp.Body);

            Handle.GET("/response2", () =>
            {
                return new Response()
                {
                    StatusCode = 203,
                    StatusDescription = "Non-Authoritative Information",
                };
            });

            resp = localNode.GET("/response2", null, null);

            resp.ResetAllCustomFields();

            Assert.IsTrue(203 == resp.StatusCode);
            Assert.IsTrue("Non-Authoritative Information" == resp.StatusDescription);
            Assert.IsTrue(null == resp.ContentType);
            Assert.IsTrue(null == resp.ContentEncoding);
            Assert.IsTrue(null == resp.SetCookie);
            Assert.IsTrue(0 == resp.ContentLength);
            Assert.IsTrue("SC" == resp["Server"]);
            Assert.IsTrue(null == resp.Body);

            Handle.GET("/response3", () =>
            {
                return new Response()
                {
                    StatusCode = 204,
                    StatusDescription = "No Content",
                };
            });

            resp = localNode.GET("/response3", null, null);

            resp.ResetAllCustomFields();

            Assert.IsTrue(204 == resp.StatusCode);
            Assert.IsTrue("No Content" == resp.StatusDescription);

            Handle.GET("/response4", () =>
            {
                return new Response()
                {
                    StatusCode = 201
                };
            });

            resp = localNode.GET("/response4", null, null);

            resp.ResetAllCustomFields();

            Assert.IsTrue(201 == resp.StatusCode);
            Assert.IsTrue("OK" == resp.StatusDescription);
        }
    }

    /// <summary>
    /// Tests user HTTP delegates registration and usage.
    /// </summary>
    [TestFixture]
    public class LocalNodeTests
    {
        class TestInfo
        {
            public String ReturnStr;
            public String TestUri;
            public String TemplateUri;

            public TestInfo(String returnStr, String testUri, String templateUri)
            {
                TemplateUri = templateUri;
                ReturnStr = returnStr;
                TestUri = testUri;
            }
        }

        /// <summary>
        /// Tests simple correct HTTP request.
        /// </summary>
        [Test]
        public void TestLocalNode()
        {
            // Node that is used for tests.
            Node localNode = new Node("127.0.0.1", 8080);
            localNode.InternalSetLocalNodeForUnitTests();

            TestInfo testInfos1  = new TestInfo("GET /@w", "/a", "/{?}");
            TestInfo testInfos2  = new TestInfo("GET /",  "/", "/");
            TestInfo testInfos3  = new TestInfo("GET /uri-with-req/@i", "/uri-with-req/123", "/uri-with-req/{?}");
            TestInfo testInfos4  = new TestInfo("GET /test", "/test", "/test");
            TestInfo testInfos5  = new TestInfo("GET /uri-with-req", "/uri-with-req", "/uri-with-req");
            TestInfo testInfos6  = new TestInfo("GET /uri-with-req/@w", "/uri-with-req/KalleKula", "/uri-with-req/{?}");
            TestInfo testInfos7  = new TestInfo("GET /admin/apapapa/@i", "/admin/apapapa/19", "/admin/apapapa/{?}");
            TestInfo testInfos8  = new TestInfo("GET /admin/@w", "/admin/KalleKula", "/admin/{?}");
            TestInfo testInfos9  = new TestInfo("GET /admin/@s/@i", "/admin/KalleKula/123", "/admin/{?}/{?}");
            TestInfo testInfos10 = new TestInfo("GET /players", "/players", "/players");
            TestInfo testInfos11 = new TestInfo("GET /players/@i/abc/@w", "/players/123/abc/John", "/players/{?}/abc/{?}");
            TestInfo testInfos12 = new TestInfo("GET /dashboard/@i", "/dashboard/123", "/dashboard/{?}");
            TestInfo testInfos13 = new TestInfo("GET /players/@i", "/players/123", "/players/{?}");
            TestInfo testInfos14 = new TestInfo("GET /players?@w", "/players?KalleKula", "/players?{?}");
            TestInfo testInfos15 = new TestInfo("GET /whatever/@s/more/@i/@w", "/whatever/apapapa/more/5547/KalleKula", "/whatever/{?}/more/{?}/{?}");
            TestInfo testInfos16 = new TestInfo("GET /ordinary", "/ordinary", "/ordinary");
            TestInfo testInfos17 = new TestInfo("GET /ordAnary", "/ordAnary", "/ordAnary");
            TestInfo testInfos18 = new TestInfo("GET /aaaaa/@i/bbbb", "/aaaaa/90510/bbbb", "/aaaaa/{?}/bbbb");
            TestInfo testInfos19 = new TestInfo("GET /whatever/@s/xxYx/@i", "/whatever/abrakadabra/xxYx/911", "/whatever/{?}/xxYx/{?}");
            TestInfo testInfos20 = new TestInfo("GET /whatever/@s/xxZx/@i", "/whatever/abrakadabra/xxZx/911", "/whatever/{?}/xxZx/{?}");
            TestInfo testInfos21 = new TestInfo("GET /whatmore/@s/xxZx/@i", "/whatmore/abrakadabra/xxZx/911", "/whatmore/{?}/xxZx/{?}");
            TestInfo testInfos22 = new TestInfo("GET /test-decimal/@m", "/test-decimal/99.123", "/test-decimal/{?}");
            TestInfo testInfos23 = new TestInfo("GET /test-double/@d", "/test-double/99.123", "/test-double/{?}");
            TestInfo testInfos24 = new TestInfo("GET /test-bool/@b", "/test-bool/true", "/test-bool/{?}");
            TestInfo testInfos25 = new TestInfo("GET /test-datatime/@t", "/test-datetime/2013-01-17", "/test-datetime/{?}");
            TestInfo testInfos26 = new TestInfo("GET /static@s/@w", "/staticmarknad/nyhetsbrev", "/static{?}/{?}");
            TestInfo testInfos27 = new TestInfo("PUT /players/@i", "/players/123", "/players/{?}");
            TestInfo testInfos28 = new TestInfo("POST /transfer?@i", "/transfer?99", "/transfer?{?}");
            TestInfo testInfos29 = new TestInfo("POST /deposit?@i", "/deposit?56754", "/deposit?{?}");
            TestInfo testInfos30 = new TestInfo("POST /find-player?firstname=@s&lastname=@s&age=@i", "/find-player?firstname=Kalle&lastname=Kula&age=19", "/find-player?firstname={?}&lastname={?}&age={?}");
            TestInfo testInfos31 = new TestInfo("DELETE /all", "/all", "/all");
            TestInfo testInfos32 = new TestInfo("GET /static/@s/static", "/static/KvaKva/static", "/static/{?}/static");
            TestInfo testInfos33 = new TestInfo("GET /@i/@b", "/657567/true", "/{?}/{?}");
            TestInfo testInfos34 = new TestInfo("GET /@i/@b/@d", "/1657567/false/-1.3457", "/{?}/{?}/{?}");
            TestInfo testInfos35 = new TestInfo("GET /@i", "/-678678", "/{?}");
            TestInfo testInfos36 = new TestInfo("GET /@i/@d", "/-75845/-1.3457", "/{?}/{?}");
            TestInfo testInfos37 = new TestInfo("GET /@i/@s/@w", "/-725845/Hello!/hello!!", "/{?}/{?}/{?}");
            TestInfo testInfos38 = new TestInfo("GET /@s/@s/@w", "/a-725845/Hello!/hello%20there!", "/{?}/{?}/{?}");
            TestInfo testInfos39 = new TestInfo("GET /@s/@i/@w", "/a-1725845/-5634673/hello%20there!", "/{?}/{?}/{?}");
            TestInfo testInfos40 = new TestInfo("GET /@i/@w", "/-4234673/somestring", "/{?}/{?}");
            TestInfo testInfos41 = new TestInfo("GET /@s/@w", "/a-45554673/somestring!", "/{?}/{?}");
            TestInfo testInfos42 = new TestInfo("GET /@i/@s/@i", "/-455673/somestring!/-1234567", "/{?}/{?}/{?}");
            TestInfo testInfos43 = new TestInfo("GET /ab", "/ab", "/ab");
            TestInfo testInfos44 = new TestInfo("GET /@s/@s/@i", "/Hej!/Hop!/-7654321", "/{?}/{?}/{?}");
            TestInfo testInfos45 = new TestInfo("GET /@s/@i", "/Hej!/-7654321", "/{?}/{?}");
            TestInfo testInfos46 = new TestInfo("GET /s@w", "/sHej!", "/s{?}");
            TestInfo testInfos47 = new TestInfo("GET /@s/static/@w", "/Hej!/static/Hop!", "/{?}/static/{?}");

            TestInfo testInfos48 = new TestInfo("GET /databases/@w", "/databases/someanother", "/databases/{?}");
            TestInfo testInfos49 = new TestInfo("GET /databases/@s?@w", "/databases/some?another", "/databases/{?}?{?}");
            TestInfo testInfos50 = new TestInfo("GET /databases", "/databases", "/databases");
            TestInfo testInfos51 = new TestInfo("GET /databases/@s/ending", "/databases/some/ending", "/databases/{?}/ending");
            TestInfo testInfos52 = new TestInfo("GET /databases/@s/else", "/databases/some2/else", "/databases/{?}/else");
            TestInfo testInfos53 = new TestInfo("POST /databases", "/databases", "/databases");
            TestInfo testInfos54 = new TestInfo("POST /databases/@w", "/databases/somestuff", "/databases/{?}");

            Response resp;

            ///////////////////////////////////////////

            Handle.GET(testInfos1.TemplateUri, (String s) => { return testInfos1.ReturnStr; });
            resp = localNode.GET(testInfos1.TestUri, null, null);
            Assert.IsTrue(testInfos1.ReturnStr == resp.GetBodyStringUtf8_Slow());

            // Uncomment for assertion failure in Codegen.
            /*
            Handle.GET(testInfos2.TemplateUri, () => { return testInfos2.ReturnStr; });
            resp = localNode.GET(testInfos2.TestUri, null, null);
            Assert.IsTrue(testInfos2.ReturnStr == resp.GetBodyStringUtf8_Slow());
            */

            ///////////////////////////////////////////

            Handle.GET(testInfos3.TemplateUri, (Request r, int i) =>
            {
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return testInfos3.ReturnStr;
            });

            resp = localNode.GET(testInfos3.TestUri, null, null);
            Assert.IsTrue(testInfos3.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos4.TemplateUri, () =>
            {
                return testInfos4.ReturnStr;
            });

            resp = localNode.GET(testInfos4.TestUri, null, null);
            Assert.IsTrue(testInfos4.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos5.TemplateUri, (Request r) =>
            {
                Assert.IsNotNull(r);
                return testInfos5.ReturnStr;
            });

            resp = localNode.GET(testInfos5.TestUri, null, null);
            Assert.IsTrue(testInfos5.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos6.TemplateUri, (string s, Request r) =>
            {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return testInfos6.ReturnStr;
            });

            resp = localNode.GET(testInfos6.TestUri, null, null);
            Assert.IsTrue(testInfos6.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos7.TemplateUri, (int i, Request r) =>
            {
                Assert.AreEqual(19, i);
                Assert.IsNotNull(r);
                return testInfos7.ReturnStr;
            });

            resp = localNode.GET(testInfos7.TestUri, null, null);
            Assert.IsTrue(testInfos7.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos8.TemplateUri, (string s, Request r) =>
            {
                Assert.AreEqual("KalleKula", s);
                Assert.IsNotNull(r);
                return testInfos8.ReturnStr;
            });

            resp = localNode.GET(testInfos8.TestUri, null, null);
            Assert.IsTrue(testInfos8.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos9.TemplateUri, (string s, int i, Request r) =>
            {
                Assert.AreEqual("KalleKula", s);
                Assert.AreEqual(123, i);
                Assert.IsNotNull(r);
                return testInfos9.ReturnStr;
            });

            resp = localNode.GET(testInfos9.TestUri, null, null);
            Assert.IsTrue(testInfos9.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos10.TemplateUri, () =>
            {
                return testInfos10.ReturnStr;
            });

            resp = localNode.GET(testInfos10.TestUri, null, null);
            Assert.IsTrue(testInfos10.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos11.TemplateUri, (int playerId, string a) =>
            {
                Assert.AreEqual(123, playerId);
                return testInfos11.ReturnStr;
            });

            resp = localNode.GET(testInfos11.TestUri, null, null);
            Assert.IsTrue(testInfos11.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos12.TemplateUri, (int playerId) =>
            {
                Assert.AreEqual(123, playerId);
                return testInfos12.ReturnStr;
            });

            resp = localNode.GET(testInfos12.TestUri, null, null);
            Assert.IsTrue(testInfos12.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos13.TemplateUri, (int id) =>
            {
                Assert.AreEqual(123, id);
                return testInfos13.ReturnStr;
            });

            resp = localNode.GET(testInfos13.TestUri, null, null);
            Assert.IsTrue(testInfos13.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos14.TemplateUri, (string fullName) =>
            {
                Assert.AreEqual("KalleKula", fullName);
                return testInfos14.ReturnStr;
            });

            resp = localNode.GET(testInfos14.TestUri, null, null);
            Assert.IsTrue(testInfos14.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos15.TemplateUri, (string v1, int v2, string v3) =>
            {
                Assert.AreEqual("apapapa", v1);
                Assert.AreEqual(5547, v2);
                Assert.AreEqual("KalleKula", v3);
                return testInfos15.ReturnStr;
            });

            resp = localNode.GET(testInfos15.TestUri, null, null);
            Assert.IsTrue(testInfos15.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos16.TemplateUri, () =>
            {
                return testInfos16.ReturnStr;
            });

            resp = localNode.GET(testInfos16.TestUri, null, null);
            Assert.IsTrue(testInfos16.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos17.TemplateUri, () =>
            {
                return testInfos17.ReturnStr;
            });

            resp = localNode.GET(testInfos17.TestUri, null, null);
            Assert.IsTrue(testInfos17.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos18.TemplateUri, (int v) =>
            {
                Assert.AreEqual(90510, v);
                return testInfos18.ReturnStr;
            });

            resp = localNode.GET(testInfos18.TestUri, null, null);
            Assert.IsTrue(testInfos18.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos19.TemplateUri, (string v1, int v2) =>
            {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return testInfos19.ReturnStr;
            });

            resp = localNode.GET(testInfos19.TestUri, null, null);
            Assert.IsTrue(testInfos19.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos20.TemplateUri, (string v1, int v2) =>
            {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return testInfos20.ReturnStr;
            });

            resp = localNode.GET(testInfos20.TestUri, null, null);
            Assert.IsTrue(testInfos20.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos21.TemplateUri, (string v1, int v2) =>
            {
                Assert.AreEqual("abrakadabra", v1);
                Assert.AreEqual(911, v2);
                return testInfos21.ReturnStr;
            });

            resp = localNode.GET(testInfos21.TestUri, null, null);
            Assert.IsTrue(testInfos21.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos22.TemplateUri, (decimal val) =>
            {
                Assert.AreEqual(99.123m, val);
                return testInfos22.ReturnStr;
            });

            resp = localNode.GET(testInfos22.TestUri, null, null);
            Assert.IsTrue(testInfos22.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos23.TemplateUri, (double val) =>
            {
                Assert.AreEqual(99.123d, val);
                return testInfos23.ReturnStr;
            });

            resp = localNode.GET(testInfos23.TestUri, null, null);
            Assert.IsTrue(testInfos23.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos24.TemplateUri, (bool val) =>
            {
                Assert.AreEqual(true, val);
                return testInfos24.ReturnStr;
            });

            resp = localNode.GET(testInfos24.TestUri, null, null);
            Assert.IsTrue(testInfos24.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            /*
            TODO: Fix datetime.
            Handle.GET(testInfos25.TemplateUri, (DateTime val) =>
            {
                DateTime expected;
                DateTime.TryParse("2013-01-17", out expected);
                Assert.AreEqual(expected, val);
                return testInfos25.ReturnStr;
            });

            resp = localNode.GET(testInfos25.TestUri, null, null);
            Assert.IsTrue(testInfos25.ReturnStr == resp.GetBodyStringUtf8_Slow());
            */

            ///////////////////////////////////////////

            Handle.GET(testInfos26.TemplateUri, (string part, string last, Request request) =>
            {
                Assert.AreEqual("marknad", part);
                Assert.AreEqual("nyhetsbrev", last);
                Assert.IsNotNull(request);
                return testInfos26.ReturnStr;
            });

            resp = localNode.GET(testInfos26.TestUri, null, null);
            Assert.IsTrue(testInfos26.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.PUT(testInfos27.TemplateUri, (int playerId) =>
            {
                Assert.AreEqual(123, playerId);
                return testInfos27.ReturnStr;
            });

            resp = localNode.PUT(testInfos27.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos27.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.POST(testInfos28.TemplateUri, (int from) =>
            {
                Assert.AreEqual(99, from);
                return testInfos28.ReturnStr;
            });

            resp = localNode.POST(testInfos28.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos28.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.POST(testInfos29.TemplateUri, (int to) =>
            {
                Assert.AreEqual(56754, to);
                return testInfos29.ReturnStr;
            });

            resp = localNode.POST(testInfos29.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos29.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.POST(testInfos30.TemplateUri, (string fn, string ln, int age) =>
            {
                Assert.AreEqual("Kalle", fn);
                Assert.AreEqual("Kula", ln);
                Assert.AreEqual(19, age);
                return testInfos30.ReturnStr;
            });

            resp = localNode.POST(testInfos30.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos30.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.DELETE(testInfos31.TemplateUri, () =>
            {
                return testInfos31.ReturnStr;
            });

            resp = localNode.DELETE(testInfos31.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos31.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            // Requests that translate in general GET string handler.

            resp = localNode.GET("/what?", null, null);
            Assert.IsTrue(testInfos1.ReturnStr == resp.GetBodyStringUtf8_Slow());

            resp = localNode.GET("/12345/12345", null, null);
            Assert.IsTrue(testInfos1.ReturnStr == resp.GetBodyStringUtf8_Slow());

            resp = localNode.GET("/some/string", null, null);
            Assert.IsTrue(testInfos1.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos32.TemplateUri, (String p1) =>
            {
                Assert.AreEqual("KvaKva", p1);
                return testInfos32.ReturnStr;
            });

            resp = localNode.GET(testInfos32.TestUri, null, null);
            Assert.IsTrue(testInfos32.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos33.TemplateUri, (Int32 p1, Boolean p2) =>
            {
                Assert.AreEqual(657567, p1);
                Assert.AreEqual(true, p2);
                return testInfos33.ReturnStr;
            });

            resp = localNode.GET(testInfos33.TestUri, null, null);
            Assert.IsTrue(testInfos33.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos34.TemplateUri, (Int32 p1, Boolean p2, Double p3) =>
            {
                Assert.AreEqual(1657567, p1);
                Assert.AreEqual(false, p2);
                Assert.AreEqual(-1.3457m, p3);

                return testInfos34.ReturnStr;
            });

            resp = localNode.GET(testInfos34.TestUri, null, null);
            Assert.IsTrue(testInfos34.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos35.TemplateUri, (Int32 p1) =>
            {
                Assert.AreEqual(-678678, p1);
                return testInfos35.ReturnStr;
            });

            resp = localNode.GET(testInfos35.TestUri, null, null);
            Assert.IsTrue(testInfos35.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos36.TemplateUri, (Int32 p1, Decimal p2) =>
            {
                Assert.AreEqual(-75845, p1);
                Assert.AreEqual(-1.3457m, p2);

                return testInfos36.ReturnStr;
            });

            resp = localNode.GET(testInfos36.TestUri, null, null);
            Assert.IsTrue(testInfos36.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos37.TemplateUri, (Int32 p1, string p2, string p3) =>
            {
                Assert.AreEqual(-725845, p1);
                Assert.AreEqual("Hello!", p2);
                Assert.AreEqual("hello!!", p3);

                return testInfos37.ReturnStr;
            });

            resp = localNode.GET(testInfos37.TestUri, null, null);
            Assert.IsTrue(testInfos37.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            // Uncomment for calling the wrong above handler.
            /*
            Handle.GET(testInfos38.TemplateUri, (string p1, string p2, string p3) =>
            {
                Assert.AreEqual("a-725845", p1);
                Assert.AreEqual("Hello!", p2);
                Assert.AreEqual("hello%20there!", p3);

                return testInfos38.ReturnStr;
            });

            resp = localNode.GET(testInfos38.TestUri, null, null);
            Assert.IsTrue(testInfos38.ReturnStr == resp.GetBodyStringUtf8_Slow());
           
            ///////////////////////////////////////////

            Handle.GET(testInfos39.TemplateUri, (string p1, Int32 p2, string p3) =>
            {
                Assert.AreEqual("a-1725845", p1);
                Assert.AreEqual(-5634673, p2);
                Assert.AreEqual("hello%20there!", p3);

                return testInfos39.ReturnStr;
            });

            resp = localNode.GET(testInfos39.TestUri, null, null);
            Assert.IsTrue(testInfos39.ReturnStr == resp.GetBodyStringUtf8_Slow());
            */

            ///////////////////////////////////////////

            Handle.GET(testInfos40.TemplateUri, (Int64 p1, string p2) =>
            {
                Assert.AreEqual(-4234673, p1);
                Assert.AreEqual("somestring", p2);

                return testInfos40.ReturnStr;
            });

            resp = localNode.GET(testInfos40.TestUri, null, null);
            Assert.IsTrue(testInfos40.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            // Uncomment for test failure.
            /*
            Handle.GET(testInfos41.TemplateUri, (string p1, string p2) =>
            {
                Assert.AreEqual("a-45554673", p1);
                Assert.AreEqual("somestring!", p2);

                return testInfos41.ReturnStr;
            });

            resp = localNode.GET(testInfos41.TestUri, null, null);
            Assert.IsTrue(testInfos41.ReturnStr == resp.GetBodyStringUtf8_Slow());
            */

            ///////////////////////////////////////////

            Handle.GET(testInfos42.TemplateUri, (Int32 p1, string p2, Int32 p3) =>
            {
                Assert.AreEqual(-455673, p1);
                Assert.AreEqual("somestring!", p2);
                Assert.AreEqual(-1234567, p3);

                return testInfos42.ReturnStr;
            });

            resp = localNode.GET(testInfos42.TestUri, null, null);
            Assert.IsTrue(testInfos42.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos43.TemplateUri, () =>
            {
                return testInfos43.ReturnStr;
            });

            resp = localNode.GET(testInfos43.TestUri, null, null);
            Assert.IsTrue(testInfos43.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos44.TemplateUri, (string p1, string p2, Int32 p3) =>
            {
                Assert.AreEqual("Hej!", p1);
                Assert.AreEqual("Hop!", p2);
                Assert.AreEqual(-7654321, p3);

                return testInfos44.ReturnStr;
            });

            resp = localNode.GET(testInfos44.TestUri, null, null);
            Assert.IsTrue(testInfos44.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos45.TemplateUri, (String p1, Int32 p2) =>
            {
                Assert.AreEqual("Hej!", p1);
                Assert.AreEqual(-7654321, p2);

                return testInfos45.ReturnStr;
            });

            resp = localNode.GET(testInfos45.TestUri, null, null);
            Assert.IsTrue(testInfos45.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos46.TemplateUri, (String p1) =>
            {
                Assert.AreEqual("Hej!", p1);

                return testInfos46.ReturnStr;
            });

            resp = localNode.GET(testInfos46.TestUri, null, null);
            Assert.IsTrue(testInfos46.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos47.TemplateUri, (string p1, string p2) =>
            {
                Assert.AreEqual("Hej!", p1);
                Assert.AreEqual("Hop!", p2);

                return testInfos47.ReturnStr;
            });

            resp = localNode.GET(testInfos47.TestUri, null, null);
            Assert.IsTrue(testInfos47.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos48.TemplateUri, (string p1) =>
            {
                Assert.AreEqual("someanother", p1);

                return testInfos48.ReturnStr;
            });

            resp = localNode.GET(testInfos48.TestUri, null, null);
            Assert.IsTrue(testInfos48.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos49.TemplateUri, (string p1, string p2) =>
            {
                Assert.AreEqual("some", p1);
                Assert.AreEqual("another", p2);

                return testInfos49.ReturnStr;
            });

            resp = localNode.GET(testInfos49.TestUri, null, null);
            Assert.IsTrue(testInfos49.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos50.TemplateUri, (Request r) =>
            {
                return testInfos50.ReturnStr;
            });

            resp = localNode.GET(testInfos50.TestUri, null, null);
            Assert.IsTrue(testInfos50.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos51.TemplateUri, (string p1) =>
            {
                Assert.AreEqual("some", p1);

                return testInfos51.ReturnStr;
            });

            resp = localNode.GET(testInfos51.TestUri, null, null);
            Assert.IsTrue(testInfos51.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.GET(testInfos52.TemplateUri, (string p1) =>
            {
                Assert.AreEqual("some2", p1);

                return testInfos52.ReturnStr;
            });

            resp = localNode.GET(testInfos52.TestUri, null, null);
            Assert.IsTrue(testInfos52.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.POST(testInfos53.TemplateUri, () =>
            {
                return testInfos53.ReturnStr;
            });

            resp = localNode.POST(testInfos53.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos53.ReturnStr == resp.GetBodyStringUtf8_Slow());

            ///////////////////////////////////////////

            Handle.POST(testInfos54.TemplateUri, (string p1) =>
            {
                Assert.AreEqual("somestuff", p1);

                return testInfos54.ReturnStr;
            });

            resp = localNode.POST(testInfos54.TestUri, (String)null, null, null);
            Assert.IsTrue(testInfos54.ReturnStr == resp.GetBodyStringUtf8_Slow());
        }
    }
}