using System;
using Starcounter.Internal;
using NUnit.Framework;

namespace FasterThanJson.Tests {
    [TestFixture]
    public class TestDecimal {
        [Test]
        public unsafe void SimpleTestDecimal() {
            fixed (byte* buffer = new byte[1 + 11]) {
                Decimal value = 2.0m;
                int size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -2.0m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 0m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 0.0007m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -1.024m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 3000;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -23500;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -0.00043m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
            }
        }

        [Test]
        public unsafe void LargeValuesTestDecimal() {
            fixed (byte* buffer = new byte[1 + 6 + 11]) {
                Decimal value = UInt32.MaxValue - 1m;
                int size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 2.0m - UInt32.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = UInt64.MaxValue + 0m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 0.0007m - UInt64.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -1.024m - UInt64.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 3000m + UInt64.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = (decimal)UInt64.MaxValue + UInt32.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = -(decimal)UInt64.MaxValue - UInt32.MaxValue;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = ((decimal)UInt64.MaxValue + UInt32.MaxValue) / 1000000000;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = (-(decimal)UInt64.MaxValue - UInt32.MaxValue) / 1000000000;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                value = 10m;
                size = Base64DecimalLossless.Write(buffer, value);
                Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                for (int i = 1; i < 28; i++) {
                    value = value / 10;
                    size = Base64DecimalLossless.Write(buffer, value);
                    Assert.AreEqual(value, Base64DecimalLossless.Read(size, buffer));
                }
            }
        }
    }
}
