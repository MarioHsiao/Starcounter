﻿using NUnit.Framework;
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

namespace Starcounter.Internal.Test {

    /// <summary>
    /// Testing correct calling levels.
    /// </summary>
    [TestFixture]
    public class TestCallLevelsClass {

        [Test]
        public void TestCallLevels() {

            Handle.GET("/level4", (Request req) => {

                Assert.IsTrue(Handle.CallLevel == 4);

                return 200;
            });

            Handle.GET("/level3", (Request req) => {

                Assert.IsTrue(Handle.CallLevel == 3);

                Response resp = X.GET("/level4");
                Assert.AreEqual(resp.IsSuccessStatusCode, true);

                return 200;
            });

            Handle.GET("/level2", (Request req) => {

                Assert.IsTrue(Handle.CallLevel == 2);

                Response resp = X.GET("/level3");
                Assert.AreEqual(resp.IsSuccessStatusCode, true);

                return 200;
            });

            Handle.GET("/level1", (Request req) => {

                Assert.IsTrue(Handle.CallLevel == 1);

                Response resp = X.GET("/level2");
                Assert.AreEqual(resp.IsSuccessStatusCode, true);

                return 200;
            });

            Response resp2 = X.GET("/level1");
            Assert.AreEqual(resp2.IsSuccessStatusCode, true);
        }

        [Test]
        public void TestOutgoingCookies() {

            Handle.AddOutgoingCookie("MyCookieName", "MyCookieValue");
            Assert.IsTrue(Handle.OutgoingCookies.Count == 1);
            Assert.IsTrue(Handle.OutgoingCookies["MyCookieName"] == "MyCookieValue");
            
            Handle.AddOutgoingCookie("myCookieName", "myCookieValue");
            Assert.IsTrue(Handle.OutgoingCookies.Count == 1);
            Assert.IsTrue(Handle.OutgoingCookies["MyCookieName"] == "myCookieValue");

            Handle.AddOutgoingCookie("mycookiename", "mycookievalue");
            Assert.IsTrue(Handle.OutgoingCookies.Count == 1);
            Assert.IsTrue(Handle.OutgoingCookies["mycookiename"] == "mycookievalue");

            Handle.AddOutgoingCookie("myanothercookiename", "myanothercookievalue");
            Assert.IsTrue(Handle.OutgoingCookies.Count == 2);
            Assert.IsTrue(Handle.OutgoingCookies["MyCookieName"] == "mycookievalue");
            Assert.IsTrue(Handle.OutgoingCookies["myanothercookiename"] == "myanothercookievalue");
        }

        [Test]
        public void TestOutgoingHeaders() {

            Handle.AddOutgoingHeader("MyHeaderName", "MyHeaderValue");
            Assert.IsTrue(Handle.OutgoingHeaders.Count == 1);
            Assert.IsTrue(Handle.OutgoingHeaders["MyHeaderName"] == "MyHeaderValue");

            Handle.AddOutgoingHeader("myHeaderName", "myHeaderValue");
            Assert.IsTrue(Handle.OutgoingHeaders.Count == 1);
            Assert.IsTrue(Handle.OutgoingHeaders["MyHeaderName"] == "myHeaderValue");

            Handle.AddOutgoingHeader("myheadername", "myheadervalue");
            Assert.IsTrue(Handle.OutgoingHeaders.Count == 1);
            Assert.IsTrue(Handle.OutgoingHeaders["myheadername"] == "myheadervalue");

            Handle.AddOutgoingHeader("myanotherheadername", "myanotherheadervalue");
            Assert.IsTrue(Handle.OutgoingHeaders.Count == 2);
            Assert.IsTrue(Handle.OutgoingHeaders["myheadername"] == "myheadervalue");
            Assert.IsTrue(Handle.OutgoingHeaders["myanotherheadername"] == "myanotherheadervalue");
        }
    }
}