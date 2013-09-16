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
                TupleWriterBase64 writeArray = new TupleWriterBase64(start, 10, 1);
                writeArray.Write((ulong)0);
                writeArray.Write((ulong)UInt32.MaxValue);
                writeArray.Write((ulong)UInt32.MinValue);
                writeArray.Write((ulong)255);
                writeArray.Write((ulong)16500);
                writeArray.Write((ulong)65500);
                writeArray.Write((ulong)7);
                writeArray.Write((ulong)(255 * 255));
                writeArray.Write((ulong)13);
                writeArray.Write((ulong)66001);
                writeArray.SealTuple();

                TupleReaderBase64 readArray = new TupleReaderBase64(start, 10);
                Assert.AreEqual(16500, readArray.ReadULong(4));
                Assert.AreEqual(65500, readArray.ReadULong(5));
                Assert.AreEqual(UInt32.MaxValue, readArray.ReadULong(1));
                Assert.AreEqual(255 * 255, readArray.ReadULong(7));
                Assert.AreEqual(UInt32.MinValue, readArray.ReadULong(2));
                Assert.AreEqual(255, readArray.ReadULong(3));
                Assert.AreEqual(0, readArray.ReadULong(0));
                Assert.AreEqual(66001, readArray.ReadULong(9));
                Assert.AreEqual(7, readArray.ReadULong(6));
                Assert.AreEqual(13, readArray.ReadULong(8));
            }
        }

        [Test]
        public unsafe void StringSimpleTest() {
            byte[] buffer = new byte[1024];
            fixed (byte* start = buffer) {
                TupleWriterBase64 writeArray = new TupleWriterBase64(start, 5, 1);
                writeArray.Write("a");
                writeArray.Write("I've verified that this has been fixed in the next branch. I will keep this issue open until we merged next into develop.");
                writeArray.Write("AAAAAA");
                writeArray.Write("");
                writeArray.Write("AAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBcccccccccccccccccccccccccccccEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEdddddddddddddddddddddddZZZZZZZZZZZZZZZZZZZZZZ");
                writeArray.SealTuple();

                TupleReaderBase64 readArray = new TupleReaderBase64(start, 5);
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
                TupleWriterBase64 writeArray = new TupleWriterBase64(start, 5, 2);
                writeArray.Write(new byte[] { byte.MinValue });
                writeArray.Write(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                writeArray.Write(new byte[] { byte.MaxValue });
                writeArray.Write(new byte[] { });
                writeArray.Write(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 });
                writeArray.SealTuple();

                TupleReaderBase64 readArray = new TupleReaderBase64(start, 5);
                Assert.AreEqual(new byte[] { byte.MinValue }, readArray.ReadByteArray(0));
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, readArray.ReadByteArray(1));
                Assert.AreEqual(new byte[] { byte.MaxValue }, readArray.ReadByteArray(2));
                Assert.AreEqual(new byte[] { }, readArray.ReadByteArray(3));
                Assert.AreEqual(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 }, readArray.ReadByteArray(4));
            }
        }

        [Test]
        public unsafe void RandomBinaryResizeTest() {
            Random rnd = new Random(5);
            byte[] inputArray1 = new byte[10];
            for (int i = 0; i < 10; i++)
                inputArray1[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            byte[] inputArray2 = new byte[100];
            for (int i = 0; i < 100; i++)
                inputArray2[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            byte[] inputArray3 = new byte[20];
            for (int i = 0; i < 20; i++)
                inputArray3[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue);
            byte[] buffer = new byte[1024];
            fixed (byte* start = buffer) {
                TupleWriterBase64 tuple = new TupleWriterBase64(start, 3, 1);
                tuple.Write(inputArray1);
                tuple.Write(inputArray2);
                tuple.Write(inputArray3);
                tuple.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 3);
                Assert.AreEqual(inputArray1, reader.ReadByteArray(0));
                Assert.AreEqual(inputArray2, reader.ReadByteArray(1));
                Assert.AreEqual(inputArray3, reader.ReadByteArray(2));
            }
        }

        [Test]
        public unsafe void SimpleResizeTest() {
            fixed (byte* start = new byte[1024]) {
                TupleWriterBase64 writeArray = new TupleWriterBase64(start, 100, 1);
                for (int i = 0; i < 100; i++)
                    writeArray.Write((ulong)16000);
                writeArray.SealTuple();
                TupleReaderBase64 readArray = new TupleReaderBase64(start, 100);
                for (int i = 0; i < 100; i++)
                    Assert.AreEqual(16000, readArray.ReadULong(i));
            }
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void RandomIndexAccessTest() {
            int nrIterations = 10000;
            Random writeRnd = new Random(1);
            for (int i = 0; i < nrIterations; i++) {
                uint nrValues = (uint)writeRnd.Next(1, 100);
                int[] valueTypes = new Int32[nrValues];
                ulong[] uintValues = new ulong[nrValues];
                String[] stringValues = new String[nrValues];
                byte[][] binaryValues = new byte[nrValues][];
                ulong[] ulongValues = new ulong[nrValues];
                long[] intValues = new long[nrValues];
                long[] longValues = new long[nrValues];
                byte[] tupleBuffer = new byte[nrValues * 700];
                fixed (byte* start = tupleBuffer) {
                    TupleWriterBase64 arrayWriter = new TupleWriterBase64(start, nrValues, 2);
                    arrayWriter.SetTupleLength((uint)tupleBuffer.Length);
                    for (int j = 0; j < nrValues; j++) {
                        valueTypes[j] = writeRnd.Next(1, 7);
                        switch (valueTypes[j]) {
                            case (int)ValueTypes.UINT:
                                uintValues[j] = RandomValues.RandomUInt(writeRnd);
                                arrayWriter.WriteSafe(uintValues[j]);
                                break;
                            case (int)ValueTypes.STRING:
                                stringValues[j] = RandomValues.RandomString(writeRnd);
                                arrayWriter.WriteSafe(stringValues[j]);
                                break;
                            case (int)ValueTypes.BINARY:
                                binaryValues[j] = RandomValues.RandomByteArray(writeRnd);
                                arrayWriter.WriteSafe(binaryValues[j]);
                                break;
                            case (int)ValueTypes.ULONG:
                                ulongValues[j] = RandomValues.RandomULong(writeRnd);
                                arrayWriter.WriteSafe(ulongValues[j]);
                                break;
                            case (int)ValueTypes.INT:
                                intValues[j] = RandomValues.RandomInt(writeRnd);
                                arrayWriter.WriteSafe(intValues[j]);
                                break;
                            case (int)ValueTypes.LONG:
                                longValues[j] = RandomValues.RandomLong(writeRnd);
                                arrayWriter.WriteSafe(longValues[j]);
                                break;
                            default:
                                Assert.Fail(((ValueTypes)valueTypes[j]).ToString());
                                break;
                        }
                    }
                    arrayWriter.SealTuple();
                }
                fixed (byte* start = tupleBuffer) {
                    TupleReaderBase64 arrayReader = new TupleReaderBase64(start, nrValues);
                    for (int j = 0; j < nrValues; j++) {
                        switch (valueTypes[j]) {
                            case (int)ValueTypes.UINT:
                                Assert.AreEqual(uintValues[j], arrayReader.ReadULong(j));
                                break;
                            case (int)ValueTypes.STRING:
                                Assert.AreEqual(stringValues[j], arrayReader.ReadString(j));
                                break;
                            case (int)ValueTypes.BINARY:
                                Assert.AreEqual(binaryValues[j], arrayReader.ReadByteArray(j));
                                break;
                            case (int)ValueTypes.ULONG:
                                Assert.AreEqual(ulongValues[j], arrayReader.ReadULong(j));
                                break;
                            case (int)ValueTypes.INT:
                                Assert.AreEqual(intValues[j], arrayReader.ReadLong(j));
                                break;
                            case (int)ValueTypes.LONG:
                                Assert.AreEqual(longValues[j], arrayReader.ReadLong(j));
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
