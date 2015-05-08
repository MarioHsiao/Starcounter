using System;
using NUnit.Framework;

namespace Starcounter.Internal.Tests {
    [TestFixture]
    public class SessionTests {
        [Test]
        public void TestSessionIdConversion() {

            ScSessionStruct s = new ScSessionStruct();
            s.Init(1, 123, 83459345345, 1);
            UInt64 lower, upper;
            ScSessionStruct.ToLowerUpper(s, out lower, out upper);

            ScSessionStruct s2 = ScSessionStruct.FromLowerUpper(lower, upper);

            Assert.AreEqual(s, s2);
        }

    }
}
