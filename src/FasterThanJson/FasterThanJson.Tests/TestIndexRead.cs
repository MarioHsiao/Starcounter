using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class TestIndexRead {
        [Test]
        public unsafe void UIntSimpleTest() {
            fixed (byte* start = new byte[1024]) {
                TupleWriterStatic writeArray = new TupleWriterStatic(start, 10, 1);
                writeArray.Write(0);
                writeArray.Write(UInt32.MaxValue);
                writeArray.Write(UInt32.MinValue);
                writeArray.Write(255);
                writeArray.Write(16500);
                writeArray.Write(65500);
                writeArray.Write(7);
                writeArray.Write(255 * 255);
                writeArray.Write(13);
                writeArray.Write(66001);
                writeArray.SealTuple();

                TupleReader readArray = new TupleReader(start, 10);
                Assert.AreEqual(16500, readArray.ReadUInt(4));
                Assert.AreEqual(65500, readArray.ReadUInt(5));
                Assert.AreEqual(UInt32.MaxValue, readArray.ReadUInt(1));
                Assert.AreEqual(255 * 255, readArray.ReadUInt(7));
                Assert.AreEqual(UInt32.MinValue, readArray.ReadUInt(2));
                Assert.AreEqual(255, readArray.ReadUInt(3));
                Assert.AreEqual(0, readArray.ReadUInt(0));
                Assert.AreEqual(66001, readArray.ReadUInt(9));
                Assert.AreEqual(7, readArray.ReadUInt(6));
                Assert.AreEqual(13, readArray.ReadUInt(8));
            }
        }

        [Test]
        public unsafe void StringSimpleTest() {
            fixed (byte* start = new byte[1024]) {
                TupleWriterStatic writeArray = new TupleWriterStatic(start, 5, 2);
                writeArray.Write("a");
                writeArray.Write("I've verified that this has been fixed in the next branch. I will keep this issue open until we merged next into develop.");
                writeArray.Write("AAAAAA");
                writeArray.Write("");
                writeArray.Write("AAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBcccccccccccccccccccccccccccccEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEdddddddddddddddddddddddZZZZZZZZZZZZZZZZZZZZZZ");
                writeArray.SealTuple();

                TupleReader readArray = new TupleReader(start, 5);
                Assert.AreEqual("AAAAAA", readArray.ReadString(2));
                Assert.AreEqual("AAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBcccccccccccccccccccccccccccccEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEdddddddddddddddddddddddZZZZZZZZZZZZZZZZZZZZZZ",
                    readArray.ReadString(4));
                Assert.AreEqual("I've verified that this has been fixed in the next branch. I will keep this issue open until we merged next into develop.", readArray.ReadString(1));
                Assert.AreEqual("a", readArray.ReadString(0));
                Assert.AreEqual("", readArray.ReadString(3));
            }

        }

        [Test]
        public unsafe void BinarySimpleTest() {
            fixed (byte* start = new byte[1024]) {
                TupleWriterStatic writeArray = new TupleWriterStatic(start, 5, 2);
                writeArray.Write(new byte[] { byte.MinValue });
                writeArray.Write(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                writeArray.Write(new byte[] { byte.MaxValue });
                writeArray.Write(new byte[] { });
                writeArray.Write(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 });
                writeArray.SealTuple();

                TupleReader readArray = new TupleReader(start, 5);
                Assert.AreEqual(new byte[] { byte.MinValue }, readArray.ReadByteArray(0));
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, readArray.ReadByteArray(1));
                Assert.AreEqual(new byte[] { byte.MaxValue }, readArray.ReadByteArray(2));
                Assert.AreEqual(new byte[] { }, readArray.ReadByteArray(3));
                Assert.AreEqual(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 }, readArray.ReadByteArray(4));
            }
        }

        [Test]
        public static unsafe void PerformanceCompare() {
            int nrIterations = 100000;
            int nrExperiments = 10;
            Random writeRnd = new Random(1);
            uint tupleSize = 10;
            uint offsetSize = 2;
            uint nr0 = RandomValues.RandomUInt(writeRnd);
            String nr1 = "I've verified that this has been fixed in the next branch. I will keep this issue open until we merged next into develop.";
            byte[] nr2 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 };
            uint nr3 = uint.MaxValue;
            uint nr4 = uint.MinValue;
            String nr5 = "AAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBcccccccccccccccccccccccccccccEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEdddddddddddddddddddddddZZZZZZZZZZZZZZZZZZZZZZ";
            String nr6 = "";
            byte[] nr7 = new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 };
            byte[] nr8 = new byte[] { byte.MinValue };
            uint nr9 = RandomValues.RandomUInt(writeRnd);
            Stopwatch timer = new Stopwatch();
            // Initialize
            TupleWriterDynamic dynamicWrite = new TupleWriterDynamic();
            for (int i = 0; i < 1; i++) {
                byte[] start = new byte[1024];
                dynamicWrite = new TupleWriterDynamic(start, 0, tupleSize, offsetSize);
                dynamicWrite.Write(nr0);
                dynamicWrite.Write(nr1);
                dynamicWrite.Write(nr2);
                dynamicWrite.Write(nr3);
                dynamicWrite.Write(nr4);
                dynamicWrite.Write(nr5);
                dynamicWrite.Write(nr6);
                dynamicWrite.Write(nr7);
                dynamicWrite.Write(nr8);
                dynamicWrite.Write(nr9);
                dynamicWrite.SealTuple();
            }
            TupleWriterStatic staticWrite = new TupleWriterStatic();
            for (int i = 0; i < 1; i++) {
                fixed (byte* start = new byte[1024]) {
                    staticWrite = new TupleWriterStatic(start, tupleSize, offsetSize);
                    staticWrite.Write(nr0);
                    staticWrite.Write(nr1);
                    staticWrite.Write(nr2);
                    staticWrite.Write(nr3);
                    staticWrite.Write(nr4);
                    staticWrite.Write(nr5);
                    staticWrite.Write(nr6);
                    staticWrite.Write(nr7);
                    staticWrite.Write(nr8);
                    staticWrite.Write(nr9);
                    staticWrite.SealTuple();
                }
            }

            long staticTime = 0;
            long dynamicTime = 0;
            for (int j = 0; j < nrExperiments; j++) {
                // Static write
                timer.Start();
                for (int i = 0; i < nrIterations; i++) {
                    fixed (byte* start = new byte[1024]) {
                        staticWrite = new TupleWriterStatic(start, tupleSize, offsetSize);
                        staticWrite.Write(nr0);
                        staticWrite.Write(nr1);
                        staticWrite.Write(nr2);
                        staticWrite.Write(nr3);
                        staticWrite.Write(nr4);
                        staticWrite.Write(nr5);
                        staticWrite.Write(nr6);
                        staticWrite.Write(nr7);
                        staticWrite.Write(nr8);
                        staticWrite.Write(nr9);
                        staticWrite.SealTuple();
                    }
                }
                timer.Stop();
                staticTime += timer.ElapsedMilliseconds;
                timer.Reset();

                // Dynamic write
                timer.Start();
                for (int i = 0; i < nrIterations; i++) {
                    byte[] start = new byte[1024];
                    dynamicWrite = new TupleWriterDynamic(start, 0, tupleSize, offsetSize);
                    dynamicWrite.Write(nr0);
                    dynamicWrite.Write(nr1);
                    dynamicWrite.Write(nr2);
                    dynamicWrite.Write(nr3);
                    dynamicWrite.Write(nr4);
                    dynamicWrite.Write(nr5);
                    dynamicWrite.Write(nr6);
                    dynamicWrite.Write(nr7);
                    dynamicWrite.Write(nr8);
                    dynamicWrite.Write(nr9);
                    dynamicWrite.SealTuple();
                }
                timer.Stop();
                dynamicTime += timer.ElapsedMilliseconds;
                timer.Reset();
            }
            Console.WriteLine("Static write - " + (double)1000 * staticTime / nrExperiments / nrIterations+ " mcs.");
            Console.WriteLine("Dynamic write - " + (double)1000 * dynamicTime / nrExperiments /nrIterations+ " mcs.");

            // Validate static
            byte* readBytes = staticWrite.AtStart;
            TupleReader readArray = new TupleReader(readBytes, tupleSize);
            Assert.AreEqual(nr0, readArray.ReadUInt(0));
            Assert.AreEqual(nr1, readArray.ReadString(1));
            Assert.AreEqual(nr2, readArray.ReadByteArray(2));
            Assert.AreEqual(nr3, readArray.ReadUInt(3));
            Assert.AreEqual(nr4, readArray.ReadUInt(4));
            Assert.AreEqual(nr5, readArray.ReadString(5));
            Assert.AreEqual(nr6, readArray.ReadString(6));
            Assert.AreEqual(nr7, readArray.ReadByteArray(7));
            Assert.AreEqual(nr8, readArray.ReadByteArray(8));
            Assert.AreEqual(nr9, readArray.ReadUInt(9));

            // Validate dynamic
            fixed (byte* start = dynamicWrite.TuplesBuffer) {
                readArray = new TupleReader(start, tupleSize);
                Assert.AreEqual(nr0, readArray.ReadUInt(0));
                Assert.AreEqual(nr1, readArray.ReadString(1));
                Assert.AreEqual(nr2, readArray.ReadByteArray(2));
                Assert.AreEqual(nr3, readArray.ReadUInt(3));
                Assert.AreEqual(nr4, readArray.ReadUInt(4));
                Assert.AreEqual(nr5, readArray.ReadString(5));
                Assert.AreEqual(nr6, readArray.ReadString(6));
                Assert.AreEqual(nr7, readArray.ReadByteArray(7));
                Assert.AreEqual(nr8, readArray.ReadByteArray(8));
                Assert.AreEqual(nr9, readArray.ReadUInt(9));
            }
        }

        [Test]
        public static unsafe void RandomIndexAccessTest() {
            int nrIterations = 10000;
            Random writeRnd = new Random(1);
            for (int i = 0; i < nrIterations; i++) {
                uint nrValues = (uint)writeRnd.Next(1, 1000);
                int[] valueTypes = new Int32[nrValues];
                uint[] uintValues = new uint[nrValues];
                String[] stringValues = new String[nrValues];
                byte[][] binaryValues = new byte[nrValues][];
                byte[] tupleBuffer = new byte[nrValues * 200];
                TupleWriterDynamic arrayWriter = new TupleWriterDynamic(tupleBuffer, 0, nrValues, 2);
                    for (int j = 0; j < nrValues; j++) {
                        valueTypes[j] = writeRnd.Next(1, 4);
                        switch (valueTypes[j]) {
                            case (int)ValueTypes.UINT:
                                uintValues[j] = RandomValues.RandomUInt(writeRnd);
                                arrayWriter.Write(uintValues[j]);
                                break;
                            case (int)ValueTypes.STRING:
                                stringValues[j] = RandomValues.RandomString(writeRnd);
                                arrayWriter.Write(stringValues[j]);
                                break;
                            case (int)ValueTypes.BINARY:
                                binaryValues[j] = RandomValues.RandomBinary(writeRnd);
                                arrayWriter.Write(binaryValues[j]);
                                break;
                            default:
                                Assert.Fail(((ValueTypes)valueTypes[j]).ToString());
                                break;
                        }
                    }
                    arrayWriter.SealTuple();
                    fixed (byte* start = tupleBuffer) {
                        TupleReader arrayReader = new TupleReader(start, nrValues);
                    for (int j = 0; j < nrValues; j++) {
                        switch (valueTypes[j]) {
                            case (int)ValueTypes.UINT:
                                Assert.AreEqual(uintValues[j], arrayReader.ReadUInt(j));
                                break;
                            case (int)ValueTypes.STRING:
                                Assert.AreEqual(stringValues[j], arrayReader.ReadString(j));
                                break;
                            case (int)ValueTypes.BINARY:
                                Assert.AreEqual(binaryValues[j], arrayReader.ReadByteArray(j));
                                break;
                            default:
                                Assert.Fail(((ValueTypes)valueTypes[j]).ToString());
                                break;
                        }
                    }
                }
            }
        }
    }
}
