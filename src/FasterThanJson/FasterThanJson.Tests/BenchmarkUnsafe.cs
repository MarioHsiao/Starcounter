﻿using System;
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
        static int[] NrElements = new int[] { 10, 100, 1000 };

        void Print(Stopwatch timer, uint nrIter, uint valueCount, string rw, string thing) {
            Console.WriteLine(rw + "ing tuple of " + valueCount + " " +
              thing + " took " + timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
              (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple " + rw + ".");
            timer.Reset();
        }

        public static int CalculateOffsetSize(int tupleLength, int valueCount) {
            int limit = 64;
            int offsetSize = 1;
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
            foreach (int valueCount in NrElements) {
                ulong[] inputUInts = new ulong[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount * 2 / 3; valCounter++) {
                    inputUInts[valCounter] = RandomValues.RandomUInt(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeULong(inputUInts[valCounter]);
                }
                for (; valCounter < valueCount; valCounter++) {
                    inputUInts[valCounter] = RandomValues.RandomULong(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeULong(inputUInts[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteULong(inputUInts[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000* 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadULong();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputUInts[j], validationReader.ReadULong());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkInt() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                long[] inputInts = new long[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount * 2 / 3; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomInt(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeLong(inputInts[valCounter]);
                }
                for (; valCounter < valueCount; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomLong(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeLong(inputInts[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteLong(inputInts[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadLong();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputInts[j], validationReader.ReadLong());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkNullableUInt() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                ulong?[] inputInts = new ulong?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount * 2 / 3; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomNullableUInt(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableULong(inputInts[valCounter]);
                }
                for (; valCounter < valueCount; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomNullableULong(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableULong(inputInts[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteULongNullable(inputInts[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadULongNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputInts[j], validationReader.ReadULongNullable());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkNullableInt() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                long?[] inputInts = new long?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount * 2 / 3; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomNullableInt(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableLong(inputInts[valCounter]);
                }
                for (; valCounter < valueCount; valCounter++) {
                    inputInts[valCounter] = RandomValues.RandomNullableLong(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableLong(inputInts[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteLongNullable(inputInts[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadLongNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " UINTs took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (uint j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputInts[j], validationReader.ReadLongNullable());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkString() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                string[] inputStrings = new string[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (int i = 0; i < valueCount; i++) {
                    inputStrings[i] = RandomValues.RandomString(rnd, strignLength);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeString(inputStrings[i]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;
                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteString(inputStrings[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " + 
                        strignLength + "-letters Strings took " + timer.ElapsedMilliseconds + 
                        " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");
                    
                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadString();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        strignLength + "-letters Strings took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputStrings[j], validationReader.ReadString());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkCharArray() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                char[][] inputStrings = new char[valueCount][];
                char[][] outputStrings = new char[valueCount][];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (int i = 0; i < valueCount; i++) {
                    inputStrings[i] = RandomValues.RandomString(rnd, strignLength).ToCharArray();
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeString(inputStrings[i]);
                    outputStrings[i] = new char[inputStrings[i].Length];
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;
                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteString(inputStrings[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " +
                        strignLength + "-letters char arrays took " + timer.ElapsedMilliseconds +
                        " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadString(outputStrings[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        strignLength + "-letters char arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++) {
                        var len = validationReader.ReadString(outputStrings[j]);
                        Assert.AreEqual(inputStrings[j].Length, len);
                        Assert.AreEqual(inputStrings[j], outputStrings[j]);
                    }
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkByteArray() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                byte[][] inputByteArrays = new byte[valueCount][];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (int i = 0; i < valueCount; i++) {
                    inputByteArrays[i] = RandomValues.RandomByteArray(rnd, byteArrayLength);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeByteArray(inputByteArrays[i]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteByteArray(inputByteArrays[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadByteArray();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationreader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputByteArrays[j], validationreader.ReadByteArray());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkByteArrayInto() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                byte[][] inputByteArrays = new byte[valueCount][];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (uint i = 0; i < valueCount; i++) {
                    inputByteArrays[i] = RandomValues.RandomByteArray(rnd, byteArrayLength);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeByteArray(inputByteArrays[i]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteByteArray(inputByteArrays[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    byte[][] outputByteArrays = new byte[valueCount][];
                    for (int i = 0; i < valueCount; i++)
                        outputByteArrays[i] = new byte[inputByteArrays[i].Length];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadByteArray(outputByteArrays[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " " +
                        byteArrayLength + "-bytes byte arrays into given took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationreader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputByteArrays[j], outputByteArrays[j]);
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkBool() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                bool[] inputBools = new bool[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount ; valCounter++) {
                    inputBools[valCounter] = RandomValues.RandomBoolean(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeBoolean(inputBools[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (int j = 0; j < valueCount; j++)
                            writer.WriteBoolean(inputBools[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Booleans took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadBoolean();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Booleans took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputBools[j], validationReader.ReadBoolean());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkBoolNullable() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                bool?[] inputBools = new bool?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputBools[valCounter] = RandomValues.RandomNullabelBoolean(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableBoolean(inputBools[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteBooleanNullable(inputBools[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Nullable Booleans took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadBooleanNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Nullable Booleans took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputBools[j], validationReader.ReadBooleanNullable());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkDecimalLossless() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                decimal[] inputDecimal = new decimal[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputDecimal[valCounter] = RandomValues.RandomDecimal(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeDecimalLossless(inputDecimal[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteDecimalLossless(inputDecimal[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadDecimalLossless();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputDecimal[j], validationReader.ReadDecimalLossless());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkDouble() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                double[] inputDouble = new double[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputDouble[valCounter] = RandomValues.RandomDouble(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeDouble(inputDouble[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteDouble(inputDouble[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Doubles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadDouble();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Doubles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputDouble[j], validationReader.ReadDouble());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkDoubleNullable() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                double?[] inputDouble = new double?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputDouble[valCounter] = RandomValues.RandomDouble(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableDouble(inputDouble[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteDoubleNullable(inputDouble[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Nullable Doubles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadDoubleNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Nullable Doubles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputDouble[j], validationReader.ReadDoubleNullable());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkX6Decimal() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                decimal[] inputDecimals = new decimal[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (int valCounter = 0; valCounter < valueCount; valCounter++) {
                    inputDecimals[valCounter] = RandomValues.RandomX6Decimal(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeX6Decimal(inputDecimals[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteX6Decimal(inputDecimals[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " X6Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            reader.ReadX6Decimal();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " X6Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputDecimals[j], validationReader.ReadX6Decimal());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkDecimalLosslessNullable() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                decimal?[] inputDecimal = new decimal?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputDecimal[valCounter] = RandomValues.RandomDecimalNullable(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableDecimalLossless(inputDecimal[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteDecimalLosslessNullable(inputDecimal[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Nullable Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadDecimalLosslessNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Nullable Decimals took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputDecimal[j], validationReader.ReadDecimalLosslessNullable());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkSingle() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                Single[] inputSingle = new Single[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputSingle[valCounter] = RandomValues.RandomSingle(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeSingle(inputSingle[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteSingle(inputSingle[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Singles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadSingle();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Singles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputSingle[j], validationReader.ReadSingle());
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void BenchmarkSingleNullable() {
            Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (int valueCount in NrElements) {
                Single?[] inputSingle = new Single?[valueCount];
                int tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                int valCounter = 0;
                for (; valCounter < valueCount; valCounter++) {
                    inputSingle[valCounter] = RandomValues.RandomSingle(rnd);
                    tupleLength += SafeTupleWriterBase64.MeasureNeededSizeNullableSingle(inputSingle[valCounter]);
                }
                int offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    int nrIter = NrIterations / valueCount;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, (uint)valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writer.WriteSingleNullable(inputSingle[j]);
                    }
                    timer.Stop();
                    Console.WriteLine("Writing tuple of " + valueCount + " Singles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 1000 * 100 / nrIter) / 100 + " mcs per tuple write.");

                    timer.Reset();
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, (uint)valueCount);
                        for (int j = 0; j < valueCount; j++)
                            reader.ReadSingleNullable();
                    }
                    timer.Stop();
                    Console.WriteLine("Reading tuple of " + valueCount + " Singles took " +
                        timer.ElapsedMilliseconds + " ms for " + nrIter + " times, i.e., " +
                        (Decimal)(timer.ElapsedMilliseconds * 100000 / nrIter) / 100 + " mcs per tuple read.");
                    TupleReaderBase64 validationReader = new TupleReaderBase64(start, (uint)valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(inputSingle[j], validationReader.ReadSingleNullable());
                }
            }
        }
#if false
        unsafe void BenchmarkInto<T>(Func<Random, T> getInputValue, Func<T, uint> getNeededSize,
            Action<TupleWriterBase64, T> writeValue, Action<TupleReaderBase64, T> readValue,
            string thing) {
                 Random rnd = new Random(1);
            Stopwatch timer = new Stopwatch();
            foreach (uint valueCount in NrElements) {
                T[] inputs = new T[valueCount];
                uint tupleLength = TupleWriterBase64.OffsetElementSizeSize;
                for (uint i = 0; i < valueCount; i++) {
                    inputs[i] = getInputValue(rnd);
                    tupleLength += getNeededSize(inputs[i]);
                }
                uint offsetSize = CalculateOffsetSize(tupleLength, valueCount);
                tupleLength += valueCount * offsetSize;
                fixed (byte* start = new byte[tupleLength]) {
                    uint nrIter = NrIterations / valueCount / 10;
                    if (TestLogger.IsRunningOnBuildServer())
                        nrIter *= 10;
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleWriterBase64 writer = new TupleWriterBase64(start, valueCount, offsetSize);
                        for (uint j = 0; j < valueCount; j++)
                            writeValue(writer, inputs[j]);
                    }
                    timer.Stop();
                    Print(timer, nrIter, valueCount, "Write", thing);
                    T[] outputs = new T[valueCount];
                    for (
                    timer.Start();
                    for (uint i = 0; i < nrIter; i++) {
                        TupleReaderBase64 reader = new TupleReaderBase64(start, valueCount);
                        for (uint j = 0; j < valueCount; j++)
                            readValue(reader, ;
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
#endif
    }
}
