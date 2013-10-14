using System;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    [TestFixture]
    public static class TestDouble {
        [Test]
        public unsafe static void TestSimpleDouble() {
            fixed (byte* buffer = new byte[11]) {
                Double value = 0;
                int size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                Assert.AreEqual(1, size);
                value = -1;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                Assert.AreEqual(2, size);
                value = 1;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                Assert.AreEqual(2, size);
                value = -0.02;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                Assert.AreEqual(11, size);
                value = 1.02;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                Assert.AreEqual(11, size);
                value = -0;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.MaxValue;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(11, size);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = value / 1000000000;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(11, size);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.MinValue;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(11, size);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = value / 1000000000;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(11, size);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.NaN;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.NegativeInfinity;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.PositiveInfinity;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = (Double)X6Decimal.MaxDecimalValue;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = (Double)X6Decimal.MinDecimalValue;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
            }
        }

        [Test]
        public unsafe static void TestSimpleDoubleNullable() {
            fixed (byte* buffer = new byte[12]) {
                Double? value = 0;
                int size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(2, size);
                value = -1;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(3, size);
                value = 1;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(3, size);
                value = -0.02;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(12, size);
                value = 1.02;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(12, size);
                value = -0;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = Double.MaxValue;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(12, size);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = value / 1000000000;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(12, size);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = Double.MinValue;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(12, size);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = value / 1000000000;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(12, size);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = Double.NaN;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = Double.NegativeInfinity;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = Double.PositiveInfinity;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = (Double)X6Decimal.MaxDecimalValue;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = (Double)X6Decimal.MinDecimalValue;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                value = null;
                size = Base64Double.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Double.ReadNullable(size, buffer));
                Assert.AreEqual(1, size);
            }
        }

        [Test]
        public unsafe static void TestSimpleSingle() {
            fixed (byte* buffer = new byte[6]) {
                Single value = 0;
                int size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                Assert.AreEqual(1, size);
                value = -1;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                Assert.AreEqual(2, size);
                value = 1;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                Assert.AreEqual(2, size);
                value = -0.02f;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                Assert.AreEqual(6, size);
                value = 1.02f;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                Assert.AreEqual(5, size);
                value = -0;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = Single.MaxValue;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = value / 100000000;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = Single.MinValue;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = value / 100000000;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = Single.NaN;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = Single.NegativeInfinity;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = Single.PositiveInfinity;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = (Single)X6Decimal.MaxDecimalValue;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
                value = (Single)X6Decimal.MinDecimalValue;
                size = Base64Single.Write(buffer, value);
                Assert.AreEqual(value, Base64Single.Read(size, buffer));
            }
        }

        [Test]
        public unsafe static void TestSimpleSingleNullable() {
            fixed (byte* buffer = new byte[8]) {
                Single? value = 0;
                int size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                Assert.AreEqual(1, size);
                value = -1;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                Assert.AreEqual(2, size);
                value = 1;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                Assert.AreEqual(2, size);
                value = -0.02f;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                Assert.AreEqual(6, size);
                value = 1.02f;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                Assert.AreEqual(5, size);
                value = -0;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = Single.MaxValue;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = value / 100000000;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = Single.MinValue;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = value / 100000000;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = Single.NaN;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(6, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = Single.NegativeInfinity;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(2, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = Single.PositiveInfinity;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(2, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = (Single)X6Decimal.MaxDecimalValue;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = (Single)X6Decimal.MinDecimalValue;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
                value = null;
                size = Base64Single.WriteNullable(buffer, value);
                Assert.AreEqual(1, size);
                Assert.AreEqual(value, Base64Single.ReadNullable(size, buffer));
            }
        }
    }
}
