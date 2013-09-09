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
                    Print(timer, "TupleWriter creates and " + valueCount + " UInt writes", nrIterations);
                    timer.Stop();
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
                    Print(timer, "TupleWriter creates and " + valueCount + " ULong writes", nrIterations);
                    timer.Stop();
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public unsafe void RunAllTests() {
            BenchmarkTupleULongScale();
            BenchmarkTupleUIntScale();
            BenchmarkTupleWriter10ULongGrow();
            BenchmarkTupleWriter10ULong();
            BenchmarkTupleWriter10UInt();
            BenchmarkTupleWriterUInt();
            BenchmarkNewTupleWriter();
            BenchmarkULong();
            BenchmarkUInt();
        }
    }
}
