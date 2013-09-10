using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class InvestigateTuplePerformance {
        const int nrIterations = 1000000;
        void Print(Stopwatch timer, string name, int nrIter) {
            Console.WriteLine(nrIter + " " + name + " took " + timer.ElapsedMilliseconds +
                " ms, i.e., " + timer.ElapsedMilliseconds * 1000000 / nrIter + " ns per iteration or " +
                nrIter * 1000 / timer.ElapsedMilliseconds + " tps.");
            timer.Reset();
        }

        //[Test]
        public unsafe void BenchmarkUInt() {
            uint value = 2341;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIterations*10; i++)
                    Base64Int.Write(buffer, value);
                timer.Stop();
            }
            Print(timer, "UInt writes", nrIterations*10);
        }

        //[Test]
        public unsafe void BenchmarkULong() {
            ulong value = UInt64.MaxValue;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIterations * 10; i++)
                    Base64Int.Write(buffer, value);
                timer.Stop();
            }
            Print(timer, "ULong writes", nrIterations * 10);
        }

        //[Test]
        public unsafe void BenchmarkNewTupleWriter() {
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIterations * 10; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 2, 2);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates", nrIterations * 10);
        }

        //[Test]
        public unsafe void BenchmarkTupleWriterUInt() {
            uint value = 2341;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIterations * 10; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 2, 2);
                    tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and UInt writes", nrIterations * 10);
        }

        //[Test]
        public unsafe void BenchmarkTupleWriter10UInt() {
            uint value = 2341;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                timer.Start();
                for (int i = 0; i < nrIterations; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 2);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 UInt writes", nrIterations);
        }

        //[Test]
        public unsafe void BenchmarkTupleWriter10ULong() {
            ulong value = UInt64.MaxValue;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[200]) {
                timer.Start();
                for (int i = 0; i < nrIterations; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 2);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 ULong writes", nrIterations);
        }

        //[Test]
        public unsafe void BenchmarkTupleWriter10ULongGrow() {
            ulong value = UInt64.MaxValue;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[200]) {
                timer.Start();
                for (int i = 0; i < nrIterations; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 1);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 ULong writes", nrIterations);
        }

        //[Test]
        public unsafe void BenchmarkTupleUIntScale() {
            uint value = 2341;
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " UInt writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadUInt());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadUInt();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " UInt reads", nrIterations);
                }
            }
        }

        //[Test]
        public unsafe void BenchmarkTupleULongScale() {
            ulong value = UInt64.MaxValue;
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[300]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " ULong writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadUInt());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadUInt();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " ULong reads", nrIterations);
                }
            }
        }

        //[Test]
        public unsafe void BenchmarkTupleString1Scale() {
            string value = "a";
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-letter String writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadString());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-letter String reads", nrIterations);
                }
            }
        }

        //[Test]
        public unsafe void BenchmarkTupleString10Scale() {
            string value = "Just text.";
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[300]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-letters String writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadString());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-letter String reads", nrIterations);
                }
            }
        }

        //[Test]
        public unsafe void BenchmarkTupleByte1Scale() {
            byte[] value = new byte[1] { 12 };
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-byte array writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadByteArray());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-byte array reads", nrIterations);
                }
            }
        }

        //[Test]
        public unsafe void BenchmarkTupleByte10Scale() {
            byte[] value = new byte[10] { 12, 255, 0, 124, 4, 0, 32, 43, 255, 231 };
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[321]) {
                foreach (uint valueCount in valueCounts) {
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-bytes array writes", nrIterations);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadByteArray());
                    timer.Start();
                    for (int i = 0; i < nrIterations; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-bytes array reads", nrIterations);
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void RunAllTests() {
            Console.WriteLine("------------ Unsigned Integers ----------------");
            BenchmarkTupleULongScale();
            BenchmarkTupleUIntScale();
            BenchmarkTupleWriter10ULongGrow();
            BenchmarkTupleWriter10ULong();
            BenchmarkTupleWriter10UInt();
            BenchmarkTupleWriterUInt();
            BenchmarkNewTupleWriter();
            BenchmarkULong();
            BenchmarkUInt();
            Console.WriteLine("------------ Strings ----------------");
            BenchmarkTupleString1Scale();
            BenchmarkTupleString10Scale();
        }
    }
}
