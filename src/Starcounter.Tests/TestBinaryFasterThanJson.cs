using System;
using Starcounter;
using Starcounter.Internal;
using NUnit.Framework;

namespace Starcounter.Tests {
    [TestFixture]
    public static class TestBinaryFasterThanJson {

        [Test]
        public unsafe static void TestBinaryTuple() {
            Binary value1 = new Binary(new byte[15] 
            {10, 0, 255, 32, 125, 
                10, 0, 255, 132, 5, 
                1, 10, 255, 32, 125});
            Binary value2 = new Binary((byte[])null);
            Binary value3 = new Binary(new byte[0]);
            fixed (byte* start = new byte[25]) {
                TupleWriterBase64 writter = new TupleWriterBase64(start, 3, 1);
                DbHelper.WriteBinary(ref writter, value1);
                DbHelper.WriteBinary(ref writter, value2);
                DbHelper.WriteBinary(ref writter, value3);
                Assert.AreEqual(25, writter.SealTuple());
                TupleReaderBase64 reader = new TupleReaderBase64(start, 3);
                Assert.AreEqual(value1, DbHelper.ReadBinary(ref reader));
                Assert.AreEqual(value2, DbHelper.ReadBinary(ref reader));
                Assert.AreEqual(value3, DbHelper.ReadBinary(ref reader));
            }
        }
        [Test]
        public unsafe static void TestSafeBinaryTuple() {
            Binary value1 = new Binary(new byte[15] 
            {10, 0, 255, 32, 125, 
                10, 0, 255, 132, 5, 
                1, 10, 255, 32, 125});
            Binary value2 = new Binary((byte[])null);
            Binary value3 = new Binary(new byte[0]);
            fixed (byte* start = new byte[25]) {
                SafeTupleWriterBase64 writter = new SafeTupleWriterBase64(start, 3, 1, 25);
                DbHelper.WriteBinary(ref writter, value3);
                DbHelper.WriteBinary(ref writter, value1);
                DbHelper.WriteBinary(ref writter, value2);
                Assert.AreEqual(25, writter.SealTuple());
                SafeTupleReaderBase64 reader = new SafeTupleReaderBase64(start, 3);
                Assert.AreEqual(value1, DbHelper.ReadBinary(ref reader, 1));
                Assert.AreEqual(value2, DbHelper.ReadBinary(ref reader, 2));
                Assert.AreEqual(value3, DbHelper.ReadBinary(ref reader, 0));
            }
        }
    }
}
