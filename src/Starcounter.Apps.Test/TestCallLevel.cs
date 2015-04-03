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

namespace Starcounter.Internal.Test {

    /// <summary>
    /// Testing correct calling levels.
    /// </summary>
    [TestFixture]
    public class TestCallLevels {

        [Test]
        public void TestCallLevelsClass() {

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
    }
}