using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Starcounter.Internal;

namespace Starcounter.Tests {
	/// <summary>
	/// 
	/// </summary>
	public static class TestDecimal {
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestConversionX6ToDecimal() {
            decimal actual;
            decimal expected;
            long encValue;
            int scale = 1000000;

            expected = 1.000000m;
            encValue = 1000000L;
            actual = X6Decimal.ToDecimal(encValue);
            Assert.AreEqual(expected, actual);

            expected = 0.000001m;
            encValue = 1L;
            actual = X6Decimal.ToDecimal(encValue);
            Assert.AreEqual(expected, actual);

            expected = 20m;
            encValue = (long)(expected*scale);
            actual = X6Decimal.ToDecimal(encValue);
            Assert.AreEqual(expected, actual);

            expected = 325346433445.456632m;
            encValue = (long)(expected * scale);
            actual = X6Decimal.ToDecimal(encValue);
            Assert.AreEqual(expected, actual);

        }

        private const int X6_WITH_SIGN_AND_SCALE = -2147090432;
        private const int X6_WO_SIGN_AND_SCALE = 393216;

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestConversionDecimalToX6() {
            decimal expected;
            decimal actual;
            int[] bits;
            long value;

            expected = 0m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = 20m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = 100m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = 32.45m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = -32.45m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WITH_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = 32.4554m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            // Data loss exception
            expected = 32.12345678m;
            Assert.Catch(() => value = X6Decimal.FromDecimal(expected));

            // Data loss exception
            expected = 32.5555555m;
            Assert.Catch(() => value = X6Decimal.FromDecimal(expected));

            // Rounding with no dataloss
            expected = 32.12345000m;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            // Above maximum allowed
            expected = 5555555555555m;
            Assert.Catch(() => value = X6Decimal.FromDecimal(expected));

            // below minimum allowed
            expected = -5555555555555m;
            Assert.Catch(() => value = X6Decimal.FromDecimal(expected));

            expected = X6Decimal.MaxDecimalValue;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WO_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale

            expected = X6Decimal.MaxDecimalValue + 1;
            Assert.Catch(() => X6Decimal.FromDecimal(expected));

            expected = X6Decimal.MinDecimalValue - 1;
            Assert.Catch(() => X6Decimal.FromDecimal(expected));

            expected = X6Decimal.MinDecimalValue;
            value = X6Decimal.FromDecimal(expected);
            actual = X6Decimal.ToDecimal(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(X6_WITH_SIGN_AND_SCALE, bits[3]); // Checking correct sign and scale
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestUnsafeReadingBits(){
            decimal dec = -854684516843215648.846m;
            Int32 unsafeLow, unsafeMid, unsafeHigh, unsafeScale;
            Int32[] bits;
            Int32 low, mid, high, scale;

            bits = decimal.GetBits(dec);
            low = bits[0];
            mid = bits[1];
            high = bits[2];
            scale = bits[3];

            unsafe {
                Int32* pbits = (Int32*)&dec;
                unsafeScale = pbits[0];
                unsafeHigh = pbits[1];
                unsafeLow = pbits[2];
                unsafeMid = pbits[3];
            }

            Assert.AreEqual(low, unsafeLow);
            Assert.AreEqual(mid, unsafeMid);
            Assert.AreEqual(high, unsafeHigh);
            Assert.AreEqual(scale, unsafeScale);
        }

		/// <summary>
        /// 
        /// </summary>
        [Test]
        public static void BenchmarkDecimalConversion() {
			decimal value;
			Int64 encodedValue;
			DateTime start;
			DateTime stop;
			int loop;

			loop = 10000000;
			value = 20.50m;

			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.FromDecimal(value);
				value = X6Decimal.ToDecimal(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");

			value = 1232353453.435346m;
			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.FromDecimal(value);
				value = X6Decimal.ToDecimal(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");
			
			value = 2001.50000000m;
			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.FromDecimal(value);
				value = X6Decimal.ToDecimal(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");
		}
    }
}
