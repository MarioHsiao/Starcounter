using System;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class TestIndexRead {
        [Test]
        public unsafe void UIntSimpleTest() {
            fixed (byte* start = new byte[1024]) {
                TupleWriter writeArray = new TupleWriter(start, 10, 1);
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
                TupleWriter writeArray = new TupleWriter(start, 5, 2);
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
                TupleWriter writeArray = new TupleWriter(start, 5, 2);
                writeArray.Write(new byte[] { byte.MinValue });
                writeArray.Write(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                writeArray.Write(new byte[] { byte.MaxValue });
                writeArray.Write(new byte[] { });
                writeArray.Write(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 });

                TupleReader readArray = new TupleReader(start, 5);
                Assert.AreEqual(new byte[] { byte.MinValue }, readArray.ReadByteArray(0));
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 }, readArray.ReadByteArray(1));
                Assert.AreEqual(new byte[] { byte.MaxValue }, readArray.ReadByteArray(2));
                Assert.AreEqual(new byte[] { }, readArray.ReadByteArray(3));
                Assert.AreEqual(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 }, readArray.ReadByteArray(4));
            }
        }

        [Test]
        public unsafe void RandomIndexAccessTest() {
            int nrIterations = 10000;
            Random writeRnd = new Random(1);
            for (int i = 0; i < nrIterations; i++) {
                uint nrValues = (uint)writeRnd.Next(1, 1000);
                int[] valueTypes = new Int32[nrValues];
                uint[] uintValues = new uint[nrValues];
                String[] stringValues = new String[nrValues];
                byte[][] binaryValues = new byte[nrValues][];
                byte[] tupleBuffer = new byte[nrValues * 200];
                    TupleWriter arrayWriter = new TupleWriter(tupleBuffer, 0, nrValues, 2);
                    for (int j = 0; j < nrValues; j++) {
                        valueTypes[j] = writeRnd.Next(1, 3);
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
