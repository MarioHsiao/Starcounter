using System;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class TestSafeTupleWriter {
        [Test]
        public unsafe void TestSafeStringWriter() {
            byte[] buffer = new byte[100];
            fixed (byte* start = buffer) {
                TupleWriter writer = new TupleWriter(start, 5, 1);

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

                TupleReader reader = new TupleReader(start, 5);
                Assert.AreEqual("abdsfklaskl;jfAKDJLKSFHA:SKFLHsadnfkalsn2354432sad", reader.ReadString(0));
                Assert.AreEqual("1234", reader.ReadString(1));
                Assert.AreEqual(" \" \n ", reader.ReadString(2));
                Assert.AreEqual("sdj  askld90we", reader.ReadString(3));
                Assert.AreEqual("Рбфюцо[å", reader.ReadString(4));
            }
        }

        [Test]
        public unsafe void TestSafeIntWriter() {
        }
    }
}
