using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.TestFramework;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class BenchmarkUnsafe {
        const int NrIterations = 1000000;
        const int strignLength = 10;
        const int byteArrayLength = 20;
        static uint[] NrElements = new uint[] { 10, 100, 1000 };

        public static uint CalculateOffsetSize(uint tupleLength, uint valueCount) {
            uint limit = 64;
            uint offsetSize = 1;
            while (tupleLength + valueCount * offsetSize >= limit) {
                limit *= 64;
                offsetSize++;
            }
            return offsetSize;
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkUInt() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (uint valueCount in NrElements) {
                ulong[] inputUInts = new ulong[valueCount];
                uint tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                uint valCounter = 0;
                for (; valCounter < valueCount * 2 / 3; valCounter++) {
                    inputUInts[valCounter] = RandomValues.RandomUInt(rnd);
                    tupleLength += TupleWriterBase64.MeasureNeededSize(inputUInts[valCounter]);
                }
                for (; valCounter < valueCount; valCounter++) {
                    inputUInts[valCounter] = RandomValues.RandomULong(rnd);
                    tupleLength += TupleWriterBase64.MeasureNeededSize(inputUInts[valCounter]);
                }
                uint offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    uint nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.Write(inputUInts[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000* 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadULong();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, valueCount);
                    for (uint j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputUInts[j], validationReader.ReadULong());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkString() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (uint valueCount in NrElements) {
                string[] inputStrings = new string[valueCount];
                uint tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (uint i = 0; i < valueCount; i++) {
                    inputStrings[i] = RandomValues.RandomString(rnd, strignLength);
                    tupleLength += TupleWriterBase64.MeasureNeededSize(inputStrings[i]);
                }
                uint offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    uint nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;
                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.Write(inputStrings[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " + 
                        strignLength + "-letters Strings took " + timer.ElapsedMilliseconds + 
                        " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");
                    
                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadString();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        strignLength + "-letters Strings took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, valueCount);
                    for (uint j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputStrings[j], validationReader.ReadString());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkByteArray() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (uint valueCount in NrElements) {
                byte[][] inputByteArrays = new byte[valueCount][];
                uint tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (uint i = 0; i < valueCount; i++) {
                    inputByteArrays[i] = RandomValues.RandomByteArray(rnd, byteArrayLength);
                    tupleLength += TupleWriterBase64.MeasureNeededSize(inputByteArrays[i]);
                }
                uint offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    uint nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.Write(inputByteArrays[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadByteArray();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");
                    TupleReaderBase64 validationreader = new TupleReaderBase64(start, valueCount);
                    for (uint j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputByteArrays[j], validationreader.ReadByteArray());
                }
            }
        }
    }
}
