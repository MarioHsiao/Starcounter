using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
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
                    int length = Base64Binary.Write(encodedPtr, valuePtr, 3);
                    Assert.AreEqual(length, 4);
                    decoded = Base64Binary.Read(4, encodedPtr);
                }
            }
            Assert.AreEqual(decoded.Length, 3);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(value[i], decoded[i]);
            byte[] nullBinary = null;
            unsafe {
                fixed (byte* encodedPtr = encoded) {
                    int length = Base64Binary.Write(encodedPtr, nullBinary);
                    Assert.AreEqual(length, 1);
                    decoded = Base64Binary.Read(1, encodedPtr);
                }
            }
            Assert.AreEqual(null, decoded);
        }

        internal void ConvertByteArray(byte[] value, int valueLength, byte[] encoded, int expectedEncodedLength) {
            byte[] decoded;
            unsafe {
                fixed (byte* valuePtr = value, encodedPtr = encoded) {
                    int length = Base64Binary.Write(encodedPtr, valuePtr, valueLength);
                    Assert.AreEqual(length, expectedEncodedLength);
                    decoded = Base64Binary.Read(expectedEncodedLength, encodedPtr);
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
                int valueLength = rnd.Next(1024);
                int valueArrayLength = valueLength;
                if (rnd.Next(0, 2) == 1)
                    valueArrayLength += rnd.Next(100);
                int encodedLength = Base64Binary.MeasureNeededSizeToEncode(valueLength);
                int encodedArrayLength = encodedLength;
                if (rnd.Next(0, 2) == 1)
                    encodedArrayLength += rnd.Next(200);
                byte[] value = new byte[valueArrayLength];
                byte[] encoded = new byte[encodedArrayLength];
                for (int j = 0; j < valueLength; j++)
                    value[j] = (byte)rnd.Next(byte.MinValue, byte.MaxValue+1);
                ConvertByteArray(value, valueLength, encoded, encodedLength);
            }
        }

        internal void BenchmarkABinary(int nrIterations, byte[] value) {
            Stopwatch timer = new Stopwatch();
            int valueLength = value.Length;
            int encodedLength = Base64Binary.MeasureNeededSizeToEncode(valueLength);
            byte[] encoded = new byte[encodedLength];
            unsafe {
                fixed (byte* encodedPtr = encoded, valuePtr = value) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++)
                        Base64Binary.Write(encodedPtr, valuePtr, valueLength);
                    timer.Stop();
                }
            }
            Console.WriteLine(nrIterations + " writes of byte array pointer with length " +
                valueLength + " takes " + timer.ElapsedMilliseconds + " ms, i.e., " +
                (1000000 * timer.ElapsedMilliseconds) / nrIterations + " ns per write.");
            timer.Reset();
            unsafe {
                fixed (byte* encodedPtr = encoded) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++)
                        Base64Binary.Write(encodedPtr, value);
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
                        Base64Binary.Read(encodedLength, encodedPtr, decodedPtr);
                    timer.Stop();
                }
            }
            for (int i=0; i<valueLength;i++)
                Assert.AreEqual(value[i], decoded[i]);
            Console.WriteLine(nrIterations + " reads of byte array with length " +
                valueLength + " takes " + timer.ElapsedMilliseconds + " ms, i.e., " +
                (1000000 * timer.ElapsedMilliseconds) / nrIterations + " ns per read.");
            timer.Reset();
            unsafe {
                fixed (byte* encodedPtr = encoded) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++)
                        decoded = Base64Binary.Read(encodedLength, encodedPtr);
                    timer.Stop();
                }
            }
            for (int i = 0; i < valueLength; i++)
                Assert.AreEqual(value[i], decoded[i]);
            Console.WriteLine(nrIterations + " reads and creates byte array with length " +
                valueLength + " takes " + timer.ElapsedMilliseconds + " ms, i.e., " +
                (1000000 * timer.ElapsedMilliseconds) / nrIterations + " ns per read.");
        }

        [Test]
        [Category("LongRunning")]
        public void BenchmarkBinaries() {
            this._BenchmarkBinaries(1000000);
        }

        [Test]
        public void TestBinaries() {
            this._BenchmarkBinaries(20);
        }

        private void _BenchmarkBinaries(int cnt) {
            Random rnd = new Random(2);
            uint valueLength = 10;
            byte[] value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(cnt, value);
            valueLength = 100;
            value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(cnt, value);
            valueLength = 1000;
            value = new byte[valueLength];
            for (int i = 0; i < valueLength; i++)
                value[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue + 1);
            BenchmarkABinary(cnt/10, value);
        }
    }
}
