using System;
using NUnit.Framework;
using Starcounter.Internal;

namespace FasterThanJson.Tests {
    [TestFixture]
    public static class TestDouble {
        [Test]
        public unsafe static void TestSimpleDouble() {
            fixed (byte* buffer = new byte[13]) {
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
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = value / 1000000000;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = Double.MinValue;
                size = Base64Double.Write(buffer, value);
                Assert.AreEqual(value, Base64Double.Read(size, buffer));
                value = value / 1000000000;
                size = Base64Double.Write(buffer, value);
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
    }
}
