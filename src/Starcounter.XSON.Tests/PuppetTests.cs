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

            Assert.AreEqual(session, root.Session);
            Assert.AreEqual(root, session.Data);

            // For single applications appname should not be used to avoid dictionary lookup.
            StarcounterEnvironment.AppName = null;
            Assert.AreEqual(root, session.Data);

            StarcounterEnvironment.AppName = "SingleApp2";
            session2.Data = root;

            Assert.IsTrue(session.Data == null);
            Assert.AreEqual(session2, root.Session);

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
