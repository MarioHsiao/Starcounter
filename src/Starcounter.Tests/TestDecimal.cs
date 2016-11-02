using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Starcounter.Internal;

namespace Starcounter.Tests {
	/// <summary>
	/// 
	/// </summary>
	public static class TestDecimal {
        private const int SCALE_6_AND_SIGN = -2147090432;
        private const int SCALE_6 = 393216;

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestConversionX6ToDecimal() {
            long v;

            v = 0L; // 0
            AssertRaw(v);

            v = 1L * 1000000; // 1
            AssertRaw(v);

            v = 0x3FFFFFFFFFFL * 1000000; // [MAX]
            AssertRaw(v);

            v = 1L * 10000; // 0.01
            AssertRaw(v);

            v = 1L; // 0.000001
            AssertRaw(v);

            v = (0x3FFFFFFFFFFL * 1000000) + (99L * 10000); // [MAX].99
            AssertRaw(v);

            v = (0x3FFFFFFFFFFL * 1000000) + 999999L; // [MAX].999999
            AssertRaw(v);

            // All pack sizes, no decimal digits.

            v = (0x1FL * 1000000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x1FFFL * 1000000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x1FFFFFL * 1000000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x1FFFFFFFL * 1000000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x1FFFFFFFFFL * 1000000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            // All pack sizes, 2 decimal digits.

            v = (0x1FL * 10000);
            AssertRaw(v);

            v += (10000);
            AssertRaw(v);

            v = (0x3FL * 1000000) + (1L * 990000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x3FFFL * 1000000) + (1L * 990000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x3FFFFFL * 1000000) + (1L * 990000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x3FFFFFFFL * 1000000) + (1L * 990000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0x3FFFFFFFFFL * 1000000) + (1L * 990000);
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            // All pack sizes, 6 decimal digits.

            v = 0x1F;
            AssertRaw(v);

            v += 1;
            AssertRaw(v);

            v = 0x1FFF;
            AssertRaw(v);

            v += 1;
            AssertRaw(v);

            v = (0L * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0xFFL * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0xFFFFL * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0xFFFFFFL * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0xFFFFFFFFL * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);

            v = (0xFFFFFFFFFFL * 1000000) + 999999L;
            AssertRaw(v);

            v += 1000000L;
            AssertRaw(v);
        }

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
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            expected = 20m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            expected = 100m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            expected = 32.45m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            expected = -32.45m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6_AND_SIGN, bits[3]); // Checking correct sign and scale

            expected = 32.4554m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            // Data loss exception
            expected = 32.12345678m;
            Assert.Catch(() => value = X6Decimal.ToEncoded(expected));

            // Data loss exception
            expected = 32.5555555m;
            Assert.Catch(() => value = X6Decimal.ToEncoded(expected));

            // Rounding with no dataloss
            expected = 32.12345000m;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            // Above maximum allowed
            expected = 5555555555555m;
            Assert.Catch(() => value = X6Decimal.ToEncoded(expected));

            // below minimum allowed
            expected = -5555555555555m;
            Assert.Catch(() => value = X6Decimal.ToEncoded(expected));

            expected = X6Decimal.MaxDecimalValue;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6, bits[3]); // Checking correct sign and scale

            expected = X6Decimal.MaxDecimalValue + 1;
            Assert.Catch(() => X6Decimal.ToEncoded(expected));

            expected = X6Decimal.MinDecimalValue - 1;
            Assert.Catch(() => X6Decimal.ToEncoded(expected));

            expected = X6Decimal.MinDecimalValue;
            value = X6Decimal.ToEncoded(expected);
            actual = X6Decimal.FromEncoded(value);
            bits = decimal.GetBits(actual);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(SCALE_6_AND_SIGN, bits[3]); // Checking correct sign and scale

            // Testing range check when high is set but not mid.
            expected = 20.555m;
            bits = decimal.GetBits(expected);
            bits[2] = 23234;
            expected = new decimal(bits);
            Assert.Catch(() => X6Decimal.ToEncoded(expected));
            
            expected = 16770000000000.0m; // United States gross national product
            Assert.Catch(() => value = X6Decimal.ToEncoded(expected));
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
        [Category("LongRunning")]
        public static void BenchmarkDecimalConversion() {
            _TestDecimalConversion(10000000);
        }

        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestDecimalConversion() {
            _TestDecimalConversion(10);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cnt"></param>
        public static void _TestDecimalConversion( int cnt ) {
            decimal value;
			Int64 encodedValue;
			DateTime start;
			DateTime stop;
			int loop;

			loop = cnt;
			value = 20.50m;

			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.ToEncoded(value);
				value = X6Decimal.FromEncoded(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");

			value = 1232353453.435346m;
			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.ToEncoded(value);
				value = X6Decimal.FromEncoded(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");
			
			value = 2001.50000000m;
			start = DateTime.Now;
			for (int i = 0; i < loop; i++) {
				encodedValue = X6Decimal.ToEncoded(value);
				value = X6Decimal.FromEncoded(encodedValue);
			}
			stop = DateTime.Now;
			Console.WriteLine("Managed decimal (" + value + "): " + (stop - start).TotalMilliseconds + " ms.");
		}
        
        /// <summary>
        /// 
        /// </summary>
        [Test]
        public static void TestEncodeDecodeX6Decimal() {
            AssertEncodeDecodeDecimal(1m);
            AssertEncodeDecodeDecimal(-1m);
            AssertEncodeDecodeDecimal(0m);
            AssertEncodeDecodeDecimal(-32.334534m);
            AssertEncodeDecodeDecimal(345344356.334534m);
            AssertEncodeDecodeDecimal(-33453453442.334534m);
            AssertEncodeDecodeDecimal(X6Decimal.MaxDecimalValue);
            AssertEncodeDecodeDecimal(X6Decimal.MinDecimalValue);
        }
 
        private static void AssertRaw(long expected) {
            decimal dec = X6Decimal.FromRaw(expected);
            long raw = X6Decimal.ToRaw(dec);
            Assert.AreEqual(expected, raw);
        }

        private static void AssertEncodeDecodeDecimal(decimal value) {
            long rawValue;
            long encodedValue;
            long decodedValue;
            int mult = 1000000;

            rawValue = X6Decimal.ToRaw(value);
            Assert.AreEqual((long)(value * mult), rawValue);
            encodedValue = X6Decimal.Encode(rawValue);
            decodedValue = X6Decimal.Decode(encodedValue);
            Assert.AreEqual(rawValue, decodedValue);
        }
    }
}
