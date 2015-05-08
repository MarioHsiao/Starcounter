using System;
using NUnit.Framework;

namespace Starcounter.Internal.Tests {
    [TestFixture]
    public class SocketTests {
        [Test]
        public void TestSessionIdConversion() {

            SocketStruct s = new SocketStruct();
            s.Init(322, 3453346456, 1);
            UInt64 lower, upper;
            SocketStruct.ToLowerUpper(s, out lower, out upper);

            SocketStruct s2 = SocketStruct.FromLowerUpper(lower, upper);

            Assert.AreEqual(s, s2);

            UInt64 x = s.ToUInt64();
            SocketStruct s3 = new SocketStruct();
            s3.FromUInt64(x);

            Assert.AreEqual(s, s3);
        }

    }
}
