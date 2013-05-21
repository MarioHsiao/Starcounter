using System;
using NUnit.Framework;

namespace Starcounter.Internal {
    [TestFixture]
    public class TestBinary {
        [Test]
        public void TestBinaryWriteSimple() {
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

        internal void ConvertByteArray(byte[] value, uint valueLength, byte[] encoded, uint expectedEncodedLength) {
            byte[] decoded;
            unsafe {
                fixed (byte* valuePtr = value, encodedPtr = encoded) {
                    uint length = Base64Binary.Write((IntPtr)encodedPtr, valuePtr, valueLength);
                    Assert.AreEqual(length, expectedEncodedLength);
                    decoded = Base64Binary.Read(expectedEncodedLength, (IntPtr)encodedPtr);
                }
            }
            Assert.AreEqual(decoded.Length, valueLength);
            for (int i = 0; i < valueLength; i++)
                Assert.AreEqual(value[i], decoded[i]);
        }

        [Test]
        public void TestConvertBinaryMaxValues() {
            byte[] value = new byte[3];
            for (int i = 0; i < 3; i++)
                value[i] = 0xFF;
            byte[] encoded = new byte[4];
            ConvertByteArray(value, 3, encoded, 4);

            value = new byte[2];
            for (int i = 0; i < 2; i++)
                value[i] = 0xFF;
            ConvertByteArray(value, 2, encoded, 3);

            value = new byte[1] {0xFF};
            ConvertByteArray(value, 1, encoded, 2);

#if false // no control of writing input
            value = new byte[4];
            for (int i = 0; i < 4; i++)
                value[i] = 0xFF;
            ConvertByteArray(value, 4, encoded, 6);
#endif
            value = new byte[6];
            for (int i = 0; i < 6; i++)
                value[i] = 0xFF;
            encoded = new byte[8];
            ConvertByteArray(value, 6, encoded, 8);

            value = new byte[4];
            for (int i = 0; i < 4; i++)
                value[i] = 0xFF;
            ConvertByteArray(value, 4, encoded, 6);

            value = new byte[5];
            for (int i = 0; i < 5; i++)
                value[i] = 0xFF;
            ConvertByteArray(value, 5, encoded, 7);
        }

        [Test]
        public void TestConvertBinaryRandom() {
            Random rnd = new Random(1);
            int nrTests = 1000;
            for (int i = 0; i < nrTests; i++) {
                uint valueLength = (uint)rnd.Next(1024);
                uint valueArrayLength = valueLength;
                if (rnd.Next(0, 1) == 1)
                    valueArrayLength += (uint)rnd.Next(100);
                uint encodedLength = Base64Binary.MeasureNeededSizeToEncode(valueLength);
                uint encodedArrayLength = encodedLength;
                if (rnd.Next(0, 1) == 1)
                    encodedArrayLength += (uint)rnd.Next(200);
                byte[] value = new byte[valueArrayLength];
                byte[] encoded = new byte[encodedArrayLength];
                for (int j = 0; j < valueLength; j++)
                    value[j] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
                ConvertByteArray(value, valueLength, encoded, encodedLength);
            }
        }
    }
}
