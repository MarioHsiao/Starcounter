using System;
using System.Diagnostics;
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
                if (rnd.Next(0, 2) == 1)
                    valueArrayLength += (uint)rnd.Next(100);
                uint encodedLength = Base64Binary.MeasureNeededSizeToEncode(valueLength);
                uint encodedArrayLength = encodedLength;
                if (rnd.Next(0, 2) == 1)
                    encodedArrayLength += (uint)rnd.Next(200);
                byte[] value = new byte[valueArrayLength];
                byte[] encoded = new byte[encodedArrayLength];
                for (int j = 0; j < valueLength; j++)
                    value[j] = (byte)rnd.Next(byte.MinValue, byte.MaxValue+1);
                ConvertByteArray(value, valueLength, encoded, encodedLength);
            }
        }

        internal void BenchmarkABinary(int nrIterations, byte[] value) {
            Stopwatch timer = new Stopwatch();
            uint valueLength = (uint)value.Length;
            uint encodedLength = Base64Binary.MeasureNeededSizeToEncode(valueLength);
            byte[] encoded = new byte[encodedLength];
            unsafe {
                fixed (byte* encodedPtr = encoded, valuePtr = value) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++)
                        Base64Binary.Write((IntPtr)encodedPtr, valuePtr, valueLength);
                    timer.Stop();
                }
            }
            Console.WriteLine(nrIterations + " writes of byte array with length " +
                valueLength + " takes " + timer.ElapsedMilliseconds + " ms, i.e., " +
                (1000000 * timer.ElapsedMilliseconds) / nrIterations + " ns per write.");
            timer.Reset();
            byte[] decoded = new byte[valueLength];
            unsafe {
                fixed (byte* decodedPtr = decoded, encodedPtr = encoded) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++)
                        Base64Binary.Read(encodedLength, (IntPtr)encodedPtr, decodedPtr);
                    timer.Stop();
                }
            }
            for (int i=0; i<valueLength;i++)
                Assert.AreEqual(value[i], decoded[i]);
            Console.WriteLine(nrIterations + " reads of byte array with length " +
                valueLength + " takes " + timer.ElapsedMilliseconds + " ms, i.e., " +
                (1000000 * timer.ElapsedMilliseconds) / nrIterations + " ns per read.");
        }

        [Test]
        public void BenchmarkBinaries() {
            Random rnd = new Random(2);
            uint valueLength = 10;
            byte[] value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(1000000, value);
            valueLength = 100;
            value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(1000000, value);
            valueLength = 1000;
            value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(100000, value);
        }
    }
}
