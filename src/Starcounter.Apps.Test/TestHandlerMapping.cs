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

namespace Starcounter.Internal.Test {

    /// <summary>
    /// Testing correct calling levels.
    /// </summary>
    [TestFixture]
    public class TestHandlerMappingClass {

        [Test]
        public void TestHandlerMapping() {

            Handle.GET("/handler1", (Request req) => {


                return "/handler1";
            });

            Handle.GET("/handler2", (Request req) => {


                return "/handler2";
            });

            UriMapping.Map("/handler1", "/sc/mapping/map1");

            Response resp = Self.GET("/handler1");
            Assert.AreEqual(resp.Body, "/handler1");

            resp = Self.GET("/sc/mapping/map1");
            Assert.AreEqual(resp.Body, "/handler1");
        }
    }
}