using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;

namespace Starcounter.Tests {
    [TestFixture]
    public static class PerformanceTest {
        static void Print(String prefix, int nrIter, Stopwatch timer) {
            Console.WriteLine(prefix + " takes " +
                timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                timer.ElapsedMilliseconds * 100000 / nrIter + " ns per conversion or " +
                nrIter * 1000 / timer.ElapsedMilliseconds + " tps");
            timer.Reset();
        }

        [Test]
        [Category("LongRunning")]
        public static void MeasureUrlGetObjectID() {
            int nrIter = 1000000;
            UInt64 objNo = UInt32.MaxValue;
            String objID = "";
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIter; i++)
                objID = DbHelper.Base64ForUrlEncode(objNo);
            timer.Stop();
            Print("Converting " + objNo + " to " + objID, nrIter, timer);
            UInt64 decodeObjNo = 0;
            timer.Start();
            for (int i = 0; i < nrIter; i++)
                decodeObjNo = DbHelper.Base64ForUrlDecode(objID);
            timer.Stop();
            Print("Converting " + objID + " to " + objNo, nrIter, timer);
            Assert.AreEqual(objNo, decodeObjNo);
        }

        [Test]
        [Category("LongRunning")]
        public static void MeasureFtjGetObjectID() {
            int nrIter = 1000000;
            UInt64 objNo = UInt32.MaxValue;
            String objID = "";
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIter; i++)
                objID = DbHelper.Base64EncodeObjectNo(objNo);
            timer.Stop();
            Print("Converting " + objNo + " to " + objID, nrIter, timer);
            UInt64 decodeObjNo = 0;
            timer.Start();
            for (int i = 0; i < nrIter; i++)
                decodeObjNo = DbHelper.Base64DecodeObjectID(objID);
            timer.Stop();
            Print("Converting " + objID + " to " + decodeObjNo, nrIter, timer);
            Assert.AreEqual(objNo, decodeObjNo);
        }
    }
}
