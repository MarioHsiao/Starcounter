using System;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class TestSafeTupleWriter {
        [Test]
        public unsafe void TestSafeStringWriter() {
            byte[] buffer = new byte[104];
            fixed (byte* start = buffer) {
                TupleWriterBase64 writer = new TupleWriterBase64(start, 5, 1);

                // Test calling WriteSafe before length is set
                Boolean wasException = false;
                try {
                    writer.WriteSafe("abc");
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRNOTUPLEWRITESAVE, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;

                // Set too little length
                try {
                    writer.SetTupleLength(2);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRBADARGUMENTS, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;

                // One proper write
                writer.SetTupleLength((uint)buffer.Length);
                writer.WriteSafe("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad");
                // Write too long value
                try {
                    writer.WriteSafe("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad");
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                // One proper value
                writer.WriteSafe("1234");
                // Too long value after resize
                try {
                    writer.WriteSafe("Klkdfajhjnc8789789721kjhdsalk    asdf");
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                // Write short enough values
                writer.WriteSafe(" \" \n ");
                writer.WriteSafe("sdj  askld90we");
                writer.WriteSafe("Рбфюцо[å");
                writer.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 5);
                Assert.AreEqual("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad", reader.ReadString(0));
                Assert.AreEqual("1234", reader.ReadString(1));
                Assert.AreEqual(" \" \n ", reader.ReadString(2));
                Assert.AreEqual("sdj  askld90we", reader.ReadString(3));
                Assert.AreEqual("Рбфюцо[å", reader.ReadString(4));
            }
        }

        [Test]
        public unsafe void TestSafeUIntWriter() {
            byte[] buffer = new byte[10];
            fixed (byte* start = buffer) {
                TupleWriterBase64 writer = new TupleWriterBase64(start, 4, 1);
                writer.SetTupleLength((uint)buffer.Length);
                writer.WriteSafe((ulong)45);
                writer.WriteSafe((ulong)256);
                Boolean wasException = false;
                try {
                    writer.WriteSafe((ulong)64 * 64 + 1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.WriteSafe((ulong)0);
                writer.WriteSafe((ulong)23);
                try {
                    writer.WriteSafe((ulong)1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 4);
                Assert.AreEqual(45, reader.ReadUInt(0));
                Assert.AreEqual(256, reader.ReadUInt(1));
                Assert.AreEqual(0, reader.ReadUInt(2));
                Assert.AreEqual(23, reader.ReadUInt(3));
                try {
                    Assert.AreEqual(1, reader.ReadUInt(4));
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
            }
        }

        [Test]
        public unsafe void TestSafeIntWriter() {
            byte[] buffer = new byte[10];
            fixed (byte* start = buffer) {
                TupleWriterBase64 writer = new TupleWriterBase64(start, 4, 1);
                writer.SetTupleLength((uint)buffer.Length);
                writer.WriteSafe(25);
                writer.WriteSafe(-256);
                Boolean wasException = false;
                try {
                    writer.WriteSafe(-64 * 64 - 1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.WriteSafe(0);
                writer.WriteSafe(-23);
                try {
                    writer.WriteSafe(1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 4);
                Assert.AreEqual(25, reader.ReadInt(0));
                Assert.AreEqual(-256, reader.ReadInt(1));
                Assert.AreEqual(0, reader.ReadInt(2));
                Assert.AreEqual(-23, reader.ReadInt(3));
                try {
                    Assert.AreEqual(1, reader.ReadInt(4));
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
            }
        }

        [Test]
        public unsafe void TestSafeBinaryWriter() {
            byte[] buffer = new byte[97]; // (97 - 17) /4 * 3 = 60 original bytes
            fixed (byte* start = buffer) {
                TupleWriterBase64 writer = new TupleWriterBase64(start, 8, 1);
                writer.SetTupleLength((uint)buffer.Length);
                byte[] value = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
                writer.WriteSafe(value); // 1 of 8 value, 14+9 bytes
                value = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                writer.WriteSafe(value); // 2 of 8 values, 28+9 of 97 bytes
                value = new byte[0];
                writer.WriteSafe(value); // 3 of 8 values, 28+9 of 97 original bytes
                value = new byte[] { 0 };
                writer.WriteSafe(value); // 4 of 8 values, 30+9 of 97 original bytes
                value = new byte[] { 255 };
                writer.WriteSafe(value); // 5 of 8 values, 32+9 of 97 original bytes
                value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                writer.WriteSafe(value); // 6 of 8 values, 59+9 of 97 original bytes
                bool wasException = false;
                try {
                    value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 17, 34, 28 };
                    writer.WriteSafe(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                writer.WriteSafe(value); // 7 of 8 values, 73 of 80 original bytes
                try {
                    value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                    writer.WriteSafe(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                value = new byte[] { 123, 4, 53, 239, 0 };
                writer.WriteSafe(value); // 8 of 8 values, 80 of 80 original bytes
                try {
                    writer.WriteSafe(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 8);
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 }, 
                    reader.ReadByteArray(0));
                Assert.AreEqual(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                    reader.ReadByteArray(1));
                Assert.AreEqual(new byte[0],
                    reader.ReadByteArray(2));
                Assert.AreEqual(new byte[] { 0 },
                    reader.ReadByteArray(3));
                Assert.AreEqual(new byte[] { 255 },
                    reader.ReadByteArray(4));
                Assert.AreEqual(new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 43, 255, 1, 13, 45},
                    reader.ReadByteArray(5));
                Assert.AreEqual(new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45 },
                    reader.ReadByteArray(6));
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                    reader.ReadByteArray(0));
                Assert.AreEqual(new byte[] { 123, 4, 53, 239, 0 },
                    reader.ReadByteArray(7));
                Assert.AreEqual(new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 },
                    reader.ReadByteArray(0));
            }
        }

        [Test]
        public unsafe void TestSafeBigUIntWriter() {
            fixed (byte* start = new byte[20]) {
                TupleWriterBase64 writer = new TupleWriterBase64(start, 3, 2);
                writer.SetTupleLength(20);
                // Too big value
                writer.WriteSafe((ulong)64 * 64 * 64 * 64 * 64 -1 + 64 * 64 * 64 * 64 * 64);
                writer.WriteSafe((ulong)64 * 64 * 64 * 64 * 64);
                writer.WriteSafe((ulong)63);
                writer.SealTuple();

                TupleReaderBase64 reader = new TupleReaderBase64(start, 3);
                Assert.AreEqual(64 * 64 * 64 * 64 * 64, reader.ReadUInt(1));
                Assert.AreEqual(63, reader.ReadUInt(2));
                Assert.AreEqual(64 * 64 * 64 * 64 * 64 - 1 + 64 * 64 * 64 * 64 * 64, reader.ReadUInt(0));
            }
        }
    }
}
