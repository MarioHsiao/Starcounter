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
                writeArray.WriteULong(0);
                writeArray.WriteULong(UInt32.MaxValue);
                writeArray.WriteULong(UInt32.MinValue);
                writeArray.WriteULong(255);
                writeArray.WriteULong(16500);
                writeArray.WriteULong(65500);
                writeArray.WriteULong(7);
                writeArray.WriteULong(255 * 255);
                writeArray.WriteULong(13);
                writeArray.WriteULong(66001);
                writeArray.SealTuple();

                SafeTupleReaderBase64 readArray = new SafeTupleReaderBase64(start, 10);
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
                writeArray.WriteString("a");
                writeArray.WriteString("I've verified that this has been fixed in the next branch. I will keep this issue open until we merged next into develop.");
                writeArray.WriteString("AAAAAA");
                writeArray.WriteString("");
                writeArray.WriteString("AAAAAAAAAAAAAAAAAAABBBBBBBBBBBBBBBBBBBBBBBBBcccccccccccccccccccccccccccccEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEdddddddddddddddddddddddZZZZZZZZZZZZZZZZZZZZZZ");
                writeArray.SealTuple();

                SafeTupleReaderBase64 readArray = new SafeTupleReaderBase64(start, 5);
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
                writeArray.WriteByteArray(new byte[] { byte.MinValue });
                writeArray.WriteByteArray(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255 });
                writeArray.WriteByteArray(new byte[] { byte.MaxValue });
                writeArray.WriteByteArray(new byte[] { });
                writeArray.WriteByteArray(new byte[] { 123, 7, 0, 12, 142, 255, 0, 0, 255, 2, 48, 129, 243, 23 });
                writeArray.SealTuple();

                SafeTupleReaderBase64 readArray = new SafeTupleReaderBase64(start, 5);
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
                tuple.WriteByteArray(inputArray1);
                tuple.WriteByteArray(inputArray2);
                tuple.WriteByteArray(inputArray3);
                tuple.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 3);
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
                    writeArray.WriteULong(16000);
                writeArray.SealTuple();
                SafeTupleReaderBase64 readArray = new SafeTupleReaderBase64(start, 100);
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
                uint nrValues = (uint)writeRnd.Next(1, 600);
                int[] valueTypes = new Int32[nrValues];
                uint[] uintValues = new uint[nrValues];
                String[] stringValues = new String[nrValues];
                byte[][] binaryValues = new byte[nrValues][];
                ulong[] ulongValues = new ulong[nrValues];
                int[] intValues = new int[nrValues];
                long[] longValues = new long[nrValues];
                uint?[] uintNullValues = new uint?[nrValues];
                ulong?[] ulongNullValues = new ulong?[nrValues];
                int?[] intNullValues = new int?[nrValues];
                long?[] longNullValues = new long?[nrValues];
                bool[] boolValues = new bool[nrValues];
                bool?[] boolNullValues = new bool?[nrValues];
                decimal[] decimalValues = new decimal[nrValues];
                decimal?[] decimalNullValues = new decimal?[nrValues];
                byte[] tupleBuffer = new byte[nrValues * 700];
                fixed (byte* start = tupleBuffer) {
                    SafeTupleWriterBase64 arrayWriter = new SafeTupleWriterBase64(start, nrValues, 2, tupleBuffer.Length);
                    for (int j = 0; j < nrValues; j++) {
                        Assert.AreEqual(15, Enum.GetValues(typeof(ValueTypes)).Length);
                        valueTypes[j] = writeRnd.Next(1, Enum.GetValues(typeof(ValueTypes)).Length);
                        switch (valueTypes[j]) {
                            case (int)ValueTypes.UINT:
                                uintValues[j] = RandomValues.RandomUInt(writeRnd);
                                arrayWriter.WriteULong(uintValues[j]);
                                break;
                            case (int)ValueTypes.STRING:
                                stringValues[j] = RandomValues.RandomString(writeRnd);
                                arrayWriter.WriteString(stringValues[j]);
                                break;
                            case (int)ValueTypes.BINARY:
                                binaryValues[j] = RandomValues.RandomByteArray(writeRnd);
                                arrayWriter.WriteByteArray(binaryValues[j]);
                                break;
                            case (int)ValueTypes.ULONG:
                                ulongValues[j] = RandomValues.RandomULong(writeRnd);
                                arrayWriter.WriteULong(ulongValues[j]);
                                break;
                            case (int)ValueTypes.INT:
                                intValues[j] = RandomValues.RandomInt(writeRnd);
                                arrayWriter.WriteLong(intValues[j]);
                                break;
                            case (int)ValueTypes.LONG:
                                longValues[j] = RandomValues.RandomLong(writeRnd);
                                arrayWriter.WriteLong(longValues[j]);
                                break;
                            case (int)ValueTypes.UINTNULL:
                                uintNullValues[j] = RandomValues.RandomNullableUInt(writeRnd);
                                arrayWriter.WriteULongNullable(uintNullValues[j]);
                                break;
                            case (int)ValueTypes.ULONGNULL:
                                ulongNullValues[j] = RandomValues.RandomNullableULong(writeRnd);
                                arrayWriter.WriteULongNullable(ulongNullValues[j]);
                                break;
                            case (int)ValueTypes.INTNULL:
                                intNullValues[j] = RandomValues.RandomNullableInt(writeRnd);
                                arrayWriter.WriteLongNullable(intNullValues[j]);
                                break;
                            case (int)ValueTypes.LONGNULL:
                                longNullValues[j] = RandomValues.RandomNullableLong(writeRnd);
                                arrayWriter.WriteLongNullable(longNullValues[j]);
                                break;
                            case (int)ValueTypes.BOOL:
                                boolValues[j] = RandomValues.RandomBoolean(writeRnd);
                                arrayWriter.WriteBoolean(boolValues[j]);
                                break;
                            case (int)ValueTypes.BOOLNULL:
                                boolNullValues[j] = RandomValues.RandomNullabelBoolean(writeRnd);
                                arrayWriter.WriteBooleanNullable(boolNullValues[j]);
                                break;
                            case (int)ValueTypes.DECIMALLOSSLESS:
                                decimalValues[j] = RandomValues.RandomDecimal(writeRnd);
                                arrayWriter.WriteDecimal(decimalValues[j]);
                                break;
                            case (int)ValueTypes.DECIMALNULL:
                                decimalNullValues[j] = RandomValues.RandomDecimalNullable(writeRnd);
                                arrayWriter.WriteDecimalNullable(decimalNullValues[j]);
                                break;
                            default:
                                Assert.Fail(((ValueTypes)valueTypes[j]).ToString());
                                break;
                        }
                    }
                    arrayWriter.SealTuple();
                }
                fixed (byte* start = tupleBuffer) {
                    SafeTupleReaderBase64 arrayReader = new SafeTupleReaderBase64(start, nrValues);
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
                            case (int)ValueTypes.UINTNULL:
                                Assert.AreEqual(uintNullValues[j], arrayReader.ReadULongNullable(j));
                                break;
                            case (int)ValueTypes.ULONGNULL:
                                Assert.AreEqual(ulongNullValues[j], arrayReader.ReadULongNullable(j));
                                break;
                            case (int)ValueTypes.INTNULL:
                                Assert.AreEqual(intNullValues[j], arrayReader.ReadLongNullable(j));
                                break;
                            case (int)ValueTypes.LONGNULL:
                                Assert.AreEqual(longNullValues[j], arrayReader.ReadLongNullable(j));
                                break;
                            case (int)ValueTypes.BOOL:
                                Assert.AreEqual(boolValues[j], arrayReader.ReadBoolean(j));
                                break;
                            case (int)ValueTypes.BOOLNULL:
                                Assert.AreEqual(boolNullValues[j], arrayReader.ReadBooleanNullable(j));
                                break;
                            case (int)ValueTypes.DECIMALLOSSLESS:
                                Assert.AreEqual(decimalValues[j], arrayReader.ReadDecimal(j));
                                break;
                            case (int)ValueTypes.DECIMALNULL:
                                Assert.AreEqual(decimalNullValues[j], arrayReader.ReadDecimalNullable(j));
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
