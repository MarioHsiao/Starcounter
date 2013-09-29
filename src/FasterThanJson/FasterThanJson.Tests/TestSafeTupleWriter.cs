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
                SafeTupleWriterBase64  writer;

                // Set too little length
                Boolean wasException = false;
                try {
                    writer = new SafeTupleWriterBase64(start, 5, 1, 2);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRBADARGUMENTS, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;

                // One proper write
                writer = new SafeTupleWriterBase64(start, 5, 1, (uint)buffer.Length);
                writer.WriteString("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad");
                // Write too long value
                try {
                    writer.WriteString("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad");
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                // One proper value
                writer.WriteString("1234");
                // Too long value after resize
                try {
                    writer.WriteString("Klkdfajhjnc8789789721kjhdsalk    asdf");
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, (uint)e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                // Write short enough values
                writer.WriteString(" \" \n ");
                writer.WriteString("sdj  askld90we");
                writer.WriteString("Рбфюцо[å");
                writer.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 5);
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
                SafeTupleWriterBase64 writer = new SafeTupleWriterBase64(start, 4, 1, (uint)buffer.Length);
                writer.WriteULong(45);
                writer.WriteULong(256);
                Boolean wasException = false;
                try {
                    writer.WriteULong(64 * 64 + 1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.WriteULong(0);
                writer.WriteULong(23);
                try {
                    writer.WriteULong(1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 4);
                Assert.AreEqual(45, reader.ReadULong(0));
                Assert.AreEqual(256, reader.ReadULong(1));
                Assert.AreEqual(0, reader.ReadULong(2));
                Assert.AreEqual(23, reader.ReadULong(3));
                try {
                    Assert.AreEqual(1, reader.ReadULong(4));
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
                SafeTupleWriterBase64 writer = new SafeTupleWriterBase64(start, 4, 1, (uint)buffer.Length);
                writer.WriteLong(25);
                writer.WriteLong(-256);
                Boolean wasException = false;
                try {
                    writer.WriteLong(-64 * 64 - 1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.WriteLong(0);
                writer.WriteLong(-23);
                try {
                    writer.WriteLong(1);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 4);
                Assert.AreEqual(25, reader.ReadLong(0));
                Assert.AreEqual(-256, reader.ReadLong(1));
                Assert.AreEqual(0, reader.ReadLong(2));
                Assert.AreEqual(-23, reader.ReadLong(3));
                try {
                    Assert.AreEqual(1, reader.ReadLong(4));
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
                SafeTupleWriterBase64 writer = new SafeTupleWriterBase64(start, 8, 1, (uint)buffer.Length);
                byte[] value = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
                writer.WriteByteArray(value); // 1 of 8 value, 14+9 bytes
                value = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                writer.WriteByteArray(value); // 2 of 8 values, 28+9 of 97 bytes
                value = new byte[0];
                writer.WriteByteArray(value); // 3 of 8 values, 28+9 of 97 original bytes
                value = new byte[] { 0 };
                writer.WriteByteArray(value); // 4 of 8 values, 30+9 of 97 original bytes
                value = new byte[] { 255 };
                writer.WriteByteArray(value); // 5 of 8 values, 32+9 of 97 original bytes
                value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                writer.WriteByteArray(value); // 6 of 8 values, 59+9 of 97 original bytes
                bool wasException = false;
                try {
                    value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 17, 34, 28 };
                    writer.WriteByteArray(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                writer.WriteByteArray(value); // 7 of 8 values, 73 of 80 original bytes
                try {
                    value = new byte[] { 123, 4, 53, 239, 0, 43, 255, 1, 13, 45,
                    123, 4, 53, 239, 0, 43, 255, 1, 13, 45};
                    writer.WriteByteArray(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                value = new byte[] { 123, 4, 53, 239, 0 };
                writer.WriteByteArray(value); // 8 of 8 values, 80 of 80 original bytes
                try {
                    writer.WriteByteArray(value);
                } catch (Exception e) {
                    Assert.AreEqual(Error.SCERRTUPLEOUTOFRANGE, e.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                writer.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 8);
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
                SafeTupleWriterBase64 writer = new SafeTupleWriterBase64(start, 3, 2, 20);
                writer.WriteULong((ulong)64 * 64 * 64 * 64 * 64 -1 + 64 * 64 * 64 * 64 * 64);
                writer.WriteULong((ulong)64 * 64 * 64 * 64 * 64);
                writer.WriteULong(63);
                writer.SealTuple();

                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 3);
                Assert.AreEqual(64 * 64 * 64 * 64 * 64, reader.ReadULong(1));
                Assert.AreEqual(63, reader.ReadULong(2));
                Assert.AreEqual(64 * 64 * 64 * 64 * 64 - 1 + 64 * 64 * 64 * 64 * 64, reader.ReadULong(0));
            }
        }

        [Test]
        public unsafe void TestSafeNestedTuple() {
            fixed (byte* start = new byte[70]) {
                SafeTupleWriterBase64 writer = new SafeTupleWriterBase64(start, 2, 1, 70);
                SafeTupleWriterBase64 nested = new SafeTupleWriterBase64(writer.AtEnd, 2, 1, 15);
                nested.WriteULong(UInt32.MaxValue);
                Boolean wasException = false;
                try {
                    nested.SealTuple();
                } catch (Exception ex) {
                    Assert.AreEqual(Error.SCERRTUPLEINCOMPLETE, ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                nested.WriteULong(UInt32.MaxValue);
                writer.HaveWritten(nested.SealTuple());
                Assert.AreEqual(nested.TupleMaxLength, nested.Length);
                Assert.AreEqual(70 - 15 - 3, writer.AvailableSize);
                try {
                    writer.SealTuple();
                } catch (Exception ex) {
                    Assert.AreEqual(Error.SCERRTUPLEINCOMPLETE, ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                SafeTupleWriterBase64 anotherNested = new SafeTupleWriterBase64(writer.AtEnd, 3, 1, 52);
                Assert.LessOrEqual(52, writer.AvailableSize);
                anotherNested.WriteByteArray(new byte[15]); // Size 20
                Assert.AreEqual(52 - 20 - 4, anotherNested.AvailableSize);
                anotherNested.WriteULong(UInt32.MaxValue);
                Assert.AreEqual(52 - 20 - 4 - 6, anotherNested.AvailableSize);
                anotherNested.WriteByteArray(new byte[16]); // Size 20
                Assert.AreEqual(0, anotherNested.AvailableSize);
                try {
                    writer.HaveWritten(anotherNested.SealTuple());
                } catch (Exception ex) {
                    Assert.AreEqual(Error.SCERRTUPLEVALUETOOBIG, ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
                try {
                    writer.SealTuple();
                } catch (Exception ex) {
                    Assert.AreEqual(Error.SCERRTUPLEINCOMPLETE, ex.Data[ErrorCode.EC_TRANSPORT_KEY]);
                    wasException = true;
                }
                Assert.True(wasException);
                wasException = false;
            }
        }
    }
}
