using System;
using System.Diagnostics;
using NUnit.Framework;
using Starcounter.Internal;
using Starcounter.TestFramework;

namespace FasterThanJson.Tests {
    [TestFixture]
    public static class InvestigateTuplePerformance {
        static int nrIterations = 100000;
        static void Print(Stopwatch timer, string name, int nrIter) {
            Console.WriteLine(nrIter + " " + name + " took " + timer.ElapsedMilliseconds +
                " ms, i.e., " + timer.ElapsedMilliseconds * 1000000 / nrIter + " ns per iteration or " +
                (long)nrIter / timer.ElapsedMilliseconds * 1000 + " tps.");
            timer.Reset();
        }

        //[Test]
        public static unsafe void BenchmarkUInt() {
            uint value = 2341;
            int nrIter = nrIterations*10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++)
                    Base64Int.Write(buffer, value);
                timer.Stop();
            }
            Print(timer, "UInt writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkULong() {
            ulong value = UInt64.MaxValue;
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++)
                    Base64Int.Write(buffer, value);
                timer.Stop();
            }
            Print(timer, "ULong writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkNewTupleWriter() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 2, 2);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleWriterUInt() {
            int nrIter = nrIterations * 10;
            uint value = 2341;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 2, 2);
                    tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and UInt writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleWriter10UInt() {
            uint value = 2341;
            int nrIter = nrIterations;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 2);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 UInt writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleWriter10ULong() {
            ulong value = UInt64.MaxValue;
            int nrIter = nrIterations;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[200]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 2);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 ULong writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleWriter10ULongGrow() {
            ulong value = UInt64.MaxValue;
            int nrIter = nrIterations;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[200]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleWriterBase64 tuple = new TupleWriterBase64(buffer, 10, 1);
                    for (int j = 0; j < 10; j++)
                        tuple.Write(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 ULong writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleUIntScale() {
            uint value = 2341;
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                for (int k =0; k< valueCounts.Length;k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " UInt writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadUInt());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadUInt();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " UInt reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleULongScale() {
            ulong value = UInt64.MaxValue;
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[300]) {
                for (int k = 0; k < valueCounts.Length; k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " ULong writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadUInt());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadUInt();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " ULong reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleString1Scale() {
            string value = "a";
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                for (int k = 0; k < valueCounts.Length; k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-letter String writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadString());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-letter String reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleString10Scale() {
            string value = "Just text.";
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[300]) {
                for (int k = 0; k < valueCounts.Length; k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-letters String writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadString());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-letter String reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleByte1Scale() {
            byte[] value = new byte[1] { 12 };
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[100]) {
                for (int k = 0; k < valueCounts.Length; k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-byte array writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadByteArray());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-byte array reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleByte10Scale() {
            byte[] value = new byte[10] { 12, 255, 0, 124, 4, 0, 32, 43, 255, 231 };
            uint[] valueCounts = new uint[] { 20, 10, 2, 1 };
            int[] nrIters = new int[] { nrIterations, nrIterations, nrIterations * 10, nrIterations * 10 };
            Assert.AreEqual(valueCounts.Length, nrIters.Length);
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[321]) {
                for (int k = 0; k < valueCounts.Length; k++) {
                    uint valueCount = valueCounts[k];
                    int nrIter = nrIters[k];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleWriterBase64 tuple = new TupleWriterBase64(buffer, valueCount, 2);
                        for (int j = 0; j < valueCount; j++)
                            tuple.Write(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-bytes array writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadByteArray());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-bytes array reads", nrIter);
                }
            }
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void RunAllTests() {
            //if (TestLogger.IsRunningOnBuildServer())
            //    nrIterations = nrIterations* 10;
#if DEBUG
            nrIterations = nrIterations / 10;
#endif
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
            Console.WriteLine("------------ Byte arrays ----------------");
            BenchmarkTupleByte1Scale();
            BenchmarkTupleByte10Scale();
        }
    }
}
