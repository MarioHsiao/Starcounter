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
    }
}
