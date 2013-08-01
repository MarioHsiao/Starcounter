using System;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
       [TestFixture]
    public class TestTupleBugs {
           [Test]
           public unsafe void Tuple1ByteArray() {
               uint testSize = 421;
               Random rnd = new Random(5);
               byte[] test = new byte[testSize];
               for (uint i = 0; i < testSize; i++)
                   test[i] = (byte)rnd.Next(byte.MinValue, byte.MaxValue+1);
               byte[] buffer = new byte[600];
               fixed (byte* start = buffer) {
                   TupleWriter writeTuple = new TupleWriter(start, 1, 2);
                   writeTuple.Write(test);
                   writeTuple.SealTuple();

                   TupleReader readTuple = new TupleReader(start, 1);
                   Assert.AreEqual(test, readTuple.ReadByteArray(0));
               }
               fixed (byte* start = buffer) {
                   TupleWriter writeTuple = new TupleWriter(start, 1, 5);
                   writeTuple.Write(test);
                   writeTuple.SealTuple();

                   TupleReader readTuple = new TupleReader(start, 1);
                   Assert.AreEqual(test, readTuple.ReadByteArray(0));
               }
           }
    }
}
