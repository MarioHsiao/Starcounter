using System;
using System.Diagnostics;
using System.Text;
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
                (long)nrIter / (timer.ElapsedMilliseconds == 0?1:timer.ElapsedMilliseconds) * 1000 + " tps.");
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
                    tuple.WriteULong(value);
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
                        tuple.WriteULong(value);
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
                        tuple.WriteULong(value);
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
                        tuple.WriteULong(value);
                }
                timer.Stop();
            }
            Print(timer, "TupleWriter creates and 10 ULong writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleUIntScale() {
            ulong value = 2341;
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
                            tuple.WriteULong(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " UInt writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadULong());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadULong();
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
                            tuple.WriteULong(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " ULong writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadULong());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadULong();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " ULong reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleIntScale() {
            int value = 1341;
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
                            tuple.WriteLong(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Int writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadLong());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadLong();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Int reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleLongScale() {
            long value = Int64.MinValue;
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
                            tuple.WriteLong(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Long writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadLong());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadLong();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Long reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkNullableUInt() {
            uint? value = 2341;
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++)
                    Base64Int.WriteNullable(buffer, value);
                timer.Stop();
            }
            Print(timer, "UInt writes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleNullableUIntScale() {
            uint? value = 2341;
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
                            tuple.WriteULongNullable(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Nullable UInt writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadULongNullable());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadULongNullable();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Nullable UInt reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleNullableULongScale() {
            ulong? value = UInt64.MaxValue;
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
                            tuple.WriteULongNullable(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Nullable ULong writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadULongNullable());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadULongNullable();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Nullable ULong reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleNullableIntScale() {
            int? value = 1341;
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
                            tuple.WriteLongNullable(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Nullable Int writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadLongNullable());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadLongNullable();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Nullable Int reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleNullableLongScale() {
            long? value = Int64.MinValue;
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
                            tuple.WriteLongNullable(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Nullable Long writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadLongNullable());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadLongNullable();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Nullable Long reads", nrIter);
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
                            tuple.WriteString(value);
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
                            tuple.WriteString(value);
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
        public static unsafe void BenchmarkTupleCharArray1Scale() {
            char[] value = "a".ToCharArray();
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
                            tuple.WriteString(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-letter char array writes", nrIter);
                    char[] res = new char[1];
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++) {
                        var len = reader.ReadString(res);
                        Assert.AreEqual(value.Length, len);
                        Assert.AreEqual(value, res);
                    }
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString(res);
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-letter char array reads", nrIter);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleCharArray10Scale() {
            char[] value = "Just text.".ToCharArray();
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
                            tuple.WriteString(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-letters char array writes", nrIter);
                    char[] res = new char[10];
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++) {
                        var len = reader.ReadString(res);
                        Assert.AreEqual(value.Length, len);
                        Assert.AreEqual(value, res);
                    }
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadString(res);
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-letter char array reads", nrIter);
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
                            tuple.WriteByteArray(value);
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
                            tuple.WriteByteArray(value);
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

        //[Test]
        public static unsafe void BenchmarkTupleByte1IntoScale() {
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
                            tuple.WriteByteArray(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 1-byte array writes", nrIter);
                    byte[] readValue = new byte[1];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray(readValue);
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 1-byte array reads into given array", nrIter);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, readValue);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkTupleByte10IntoScale() {
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
                            tuple.WriteByteArray(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " 10-bytes array writes", nrIter);
                    byte[] readValue = new byte[10];
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadByteArray(readValue);
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " 10-bytes array reads into given array", nrIter);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, readValue);
                }
            }
        }

        //[Test]
        public static unsafe void BenchmarkNewTupleReader() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[10]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    TupleReaderBase64 tuple = new TupleReaderBase64(buffer, 2);
                }
                timer.Stop();
            }
            Print(timer, "TupleReader creates", nrIter);
        }

        static unsafe void AllocateBuffer(int size) {
            char* buffer = stackalloc char[size];
        }

        //[Test]
        public static unsafe void BenchmarkBufferAllocation() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIter; i++) {
                AllocateBuffer(10);
            }
            timer.Stop();
            Print(timer, "String buffer allocation", nrIter);
        }

        static Encoder Utf8Encode = new UTF8Encoding(false, true).GetEncoder();
        static Decoder Utf8Decode = new UTF8Encoding(false, true).GetDecoder();

        //[Test]
        public static unsafe void BenchmarkGetBytes() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            fixed (byte* start = new byte[20]) {
                String str = "Just text.";
                fixed (char* pStr = str) {
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        int len = Utf8Encode.GetBytes(pStr, 10, start, 20, true); ;
                    }
                    timer.Stop();
                }
            }
            Print(timer, "Get bytes", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkGetChars() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            char* buffer = stackalloc char[11];
            fixed (byte* start = new byte[20]) {
                String str = "Just text.";
                fixed (char* pStr = str) {
                    int len = Utf8Encode.GetBytes(pStr, 10, start, 20, true); ;
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        Utf8Decode.GetChars(start, len, buffer, 10, true);
                    }
                    timer.Stop();
                }
            }
            Print(timer, "Get chars", nrIter);
        }


        //[Test]
        public static unsafe void BenchmarkNewString() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            //char* buffer = stackalloc char[11];
            //buffer = "Just text.";
            String str = "Just text.";
            fixed (char* buffer = str.ToCharArray()) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    str = new String(buffer, 0, 10);
                }
                timer.Stop();
            }
            Print(timer, "New String", nrIter);
        }

        static unsafe void NewStringFromBufferAlloc(int size) {
            char* buffer = stackalloc char[size + 1];
            String str = new String(buffer, 0, size);
        }

        //[Test]
        public static unsafe void BenchmarkNewStringBufferAlloc() {
            int nrIter = nrIterations * 10;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < nrIter; i++) {
                NewStringFromBufferAlloc(10);
            }
            timer.Stop();
            Print(timer, "New String with buffer allocation", nrIter);
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void BenchmarkWriteBase64x1() {
            int nrIter = nrIterations * 100;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[1]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    Base64Int.WriteBase64x1(32, buffer);
                }
                timer.Stop();
                Assert.AreEqual(32, Base64Int.ReadBase64x1(buffer));
            }
            Print(timer, "Write Int Base64x1", nrIter);
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void BenchmarkReadBase64x1() {
            int nrIter = nrIterations * 100;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[1]) {
                Base64Int.WriteBase64x1(32, buffer);
                ulong res = 0;
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    res = Base64Int.ReadBase64x1(buffer);
                }
                timer.Stop();
                Assert.AreEqual(32, res);
            }
            Print(timer, "Read Int Base64x1", nrIter);
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void BenchmarkWriteBase64x2() {
            int nrIter = nrIterations * 100;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[1]) {
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    Base64Int.WriteBase64x2(100, buffer);
                }
                timer.Stop();
                Assert.AreEqual(100, Base64Int.ReadBase64x2(buffer));
            }
            Print(timer, "Write Int Base64x2", nrIter);
        }

        [Test]
        [Category("LongRunning")]
        public static unsafe void BenchmarkReadBase64x2() {
            int nrIter = nrIterations * 100;
            Stopwatch timer = new Stopwatch();
            fixed (byte* buffer = new byte[1]) {
                Base64Int.WriteBase64x2(100, buffer);
                ulong res = 0;
                timer.Start();
                for (int i = 0; i < nrIter; i++) {
                    res = Base64Int.ReadBase64x2(buffer);
                }
                timer.Stop();
                Assert.AreEqual(100, res);
            }
            Print(timer, "Read Int Base64x2", nrIter);
        }

        //[Test]
        public static unsafe void BenchmarkTupleBoolScale() {
            bool value = true;
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
                            tuple.WriteBoolean(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Boolean writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadBoolean());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadBoolean();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Boolean reads", nrIter);
                }
            }
        }

        [Test]
        public static unsafe void BenchmarkTupleDecimalLosslessScale() {
            decimal value = 100m;
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
                            tuple.WriteDecimalLossless(value);
                    }
                    timer.Stop();
                    Print(timer, "TupleWriter creates and " + valueCount + " Decimal writes", nrIter);
                    TupleReaderBase64 reader = new TupleReaderBase64(buffer, valueCount);
                    for (int j = 0; j < valueCount; j++)
                        Assert.AreEqual(value, reader.ReadDecimalLossless());
                    timer.Start();
                    for (int i = 0; i < nrIter; i++) {
                        TupleReaderBase64 tuple = new TupleReaderBase64(buffer, valueCount);
                        for (int j = 0; j < valueCount; j++)
                            tuple.ReadDecimalLossless();
                    }
                    timer.Stop();
                    Print(timer, "TupleReader creates and " + valueCount + " Decimal reads", nrIter);
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
            Console.WriteLine("------------ Signed Integers ----------------");
            BenchmarkTupleLongScale();
            BenchmarkTupleIntScale();
            Console.WriteLine("------------ Nullable unsigned Integers ----------------");
            BenchmarkTupleNullableULongScale();
            BenchmarkTupleNullableUIntScale();
            BenchmarkNullableUInt();
            Console.WriteLine("------------ Nullable signed Integers ----------------");
            BenchmarkTupleNullableLongScale();
            BenchmarkTupleNullableIntScale();
            Console.WriteLine("------------ Strings ----------------");
            BenchmarkTupleString10Scale();
            BenchmarkTupleString1Scale();
            BenchmarkTupleCharArray10Scale();
            BenchmarkTupleCharArray1Scale();
            BenchmarkGetBytes();
            BenchmarkNewStringBufferAlloc();
            BenchmarkGetChars();
            BenchmarkNewString();
            BenchmarkBufferAllocation();
            BenchmarkNewTupleReader();
            Console.WriteLine("------------ Byte arrays ----------------");
            BenchmarkTupleByte10Scale();
            BenchmarkTupleByte1Scale();
            BenchmarkTupleByte10IntoScale();
            BenchmarkTupleByte1IntoScale();
            Console.WriteLine("------------ Booleans ----------------");
            BenchmarkTupleBoolScale();
        }
    }
}
