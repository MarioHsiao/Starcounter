using System;
using NUnit.Framework;

namespace Starcounter.Internal {
    [TestFixture]
    public class TestBinary {
        [Test]
        public void TestBinaryWrite() {
            byte[] value = new byte[3];
            for (int i = 0; i <3; i++)
                value[i]= 0xFF;
            byte[] encoded = new byte[4];
            byte[] decoded;
            unsafe {
                fixed (byte* valuePtr = value, encodedPtr = encoded) {
                    uint length = Base64Binary.Write((IntPtr)encodedPtr, valuePtr, 3);
                    Assert.AreEqual(length, 4);
                    decoded = Base64Binary.Read(4, (IntPtr)encodedPtr);
                }
            }
            Assert.AreEqual(decoded.Length, 3);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(value[i], decoded[i]);
        }
    }
}
