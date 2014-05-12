using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Starcounter.Internal.XSON.Tests {
    [TestFixture]
    public class PuppetTests {

        [TestFixtureSetUp]
        public static void Setup() {
            GlobalSessions.InitGlobalSessions(1);
            StarcounterEnvironment.AppName = "Test";
        }

        [TearDown]
        public static void TearDown() {
            StarcounterEnvironment.AppName = null;
        }

        [Test]
        public static void TestSingleSessionState() {
            Json root = new Json();
            Session session = new Session();
            Session session2 = new Session();

            session.CargoId = 1;
            session2.CargoId = 2;

            StarcounterEnvironment.AppName = "SingleApp";
            session.Data = root;

            Assert.IsTrue(session == root.Session);
            Assert.IsTrue(root == session.Data);

            StarcounterEnvironment.AppName = "SingleApp2";
            session2.Data = root;

            Assert.IsTrue(session.Data == null);
            Assert.IsTrue(session2 == root.Session);

            StarcounterEnvironment.AppName = "Test";
        }

        [Test]
        public static void TestMultiAppSessionState() {
            var root1 = new Json();
            var root2 = new Json();
            var root3 = new Json();
            var app1 = "App1";
            var app2 = "App2";
            var app3 = "App3";
            var session = new Session();

            StarcounterEnvironment.AppName = app1;
            session.Data = root1;

            StarcounterEnvironment.AppName = app2;
            session.Data = root2;

            StarcounterEnvironment.AppName = app3;
            session.Data = root3;

            StarcounterEnvironment.AppName = app2;
            Assert.IsTrue(root2 == session.Data);

            StarcounterEnvironment.AppName = app1;
            Assert.IsTrue(root1 == session.Data);

            StarcounterEnvironment.AppName = app3;
            Assert.IsTrue(root3 == session.Data);

            StarcounterEnvironment.AppName = "Test";
        }

        [Test]
        public static void TestIncorrectRootObjectForSession() {
            Exception ex;
            uint ec;
            dynamic root = new Json();
            Json child = new Json();
            Session session = new Session();

            root.Child = child;

            // Cannot set json that is not a root as sessionstate.
            ex = Assert.Throws<Exception>(() => { child.Session = session; });
            ErrorCode.TryGetCode(ex, out ec);
            Assert.AreEqual(Error.SCERRSESSIONJSONNOTROOT, ec);

            ex = Assert.Throws<Exception>(() => { session.Data = child; });
            ErrorCode.TryGetCode(ex, out ec);
            Assert.AreEqual(Error.SCERRSESSIONJSONNOTROOT, ec);
        }
    }
}
