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
		/// Function used by DecimalTest().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt16 convert_x6_decimal_to_clr_decimal
		(UInt64 record_id, UInt64 record_addr, Int32 column_index, Int32* decimal_part_ptr);

		/// <summary>
		/// Function used by DecimalTest().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 convert_clr_decimal_to_x6_decimal
		(UInt64 record_id, UInt64 record_addr, Int32 column_index,
		Int32 low, Int32 middle, Int32 high, Int32 scale_sign);

		/// <summary>
		/// Function used by DecimalTest().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 clr_decimal_to_encoded_x6_decimal
		(Int32* decimal_part_ptr, ref Int64 encoded_x6_decimal_ptr);
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public static void DecimalTest() {
			Decimal clrDecimal = 0;
			Int64 encodedX6Decimal;

			unsafe {
				Int32[] decimalPart = Decimal.GetBits(clrDecimal);
				encodedX6Decimal = 0;

				fixed (Int32* decimalPartPtr = decimalPart) {
					// clr_decimal_to_encoded_x6_decimal() will do the conversion, and if the value fits
					// without data loss, the value will be written to encodedX6Decimal.
					Assert.AreEqual(0, clr_decimal_to_encoded_x6_decimal(decimalPartPtr, ref encodedX6Decimal));
				}
			}

			Assert.True(true);
		}
    }
}
