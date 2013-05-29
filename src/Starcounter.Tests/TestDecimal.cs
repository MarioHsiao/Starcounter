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
		/// Function used by DecimalTest(). NOTE: This is a modified copy of the real
		/// function convert_x6_decimal_to_clr_decimal() with the difference that it
		/// does not call sccoredb_get_encdec(), since that requires a database.
		/// But the unit test must be changed later to use convert_x6_decimal_to_clr_decimal().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt16 convert_x6_decimal_to_clr_decimal_test
		(Int32* decimal_part_ptr, Int64 encoded_value);

		/// <summary>
		/// Function used by DecimalTest(). NOTE: This is a modified copy of the real
		/// function convert_clr_decimal_to_x6_decimal() with the difference that it
		/// does not call sccoredb_put_encdec(), since that requires a database.
		/// But the unit test must be changed later to use convert_clr_decimal_to_x6_decimal().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 convert_clr_decimal_to_x6_decimal_test
		(Int32 low, Int32 middle, Int32 high, Int32 scale_sign, Int64* encoded_value);

		/// <summary>
		/// Function used by DecimalTest(). NOTE: This uses the real function because it
		/// does not require a database.
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 clr_decimal_to_encoded_x6_decimal
		(Int32* decimal_part_ptr, ref Int64 encoded_x6_decimal_ptr);
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public static void DecimalTest() {
			//==================================================================
			// Test ClrDecimalToEncodedX6Decimal()
			//==================================================================
			{
				Decimal d = 0;
				Int64 encodedX6Decimal = DbState.ClrDecimalToEncodedX6Decimal(d);
				Assert.AreEqual(encodedX6Decimal, 0);
			}

			//Assert.AreEqual(true, ClrDecimalToEncodedX6Decimal());
			Assert.True(true);
		}

#if false
		/// <summary>
		/// 
		/// </summary>
		public static Decimal ReadDecimal() {
			Int64 encodedX6Decimal;

			unsafe {
				Int32[] decimalPart = Decimal.GetBits(clrDecimal);
				encodedX6Decimal = 0;

				fixed (Int32* decimalPartPtr = decimalPart) {
					// clr_decimal_to_encoded_x6_decimal() will do the conversion, and if the value fits
					// without data loss, the value will be written to encodedX6Decimal.
					return clr_decimal_to_encoded_x6_decimal(decimalPartPtr, ref encodedX6Decimal) == 0;
				}
			}

			///------------
			UInt16 flags;

			unsafe {
				Int32[] decimalPart = new Int32[4];
				Int64 encodedValue;

				fixed (Int32* decimalPartPtr = decimalPart) {
					flags = convert_x6_decimal_to_clr_decimal_test(decimalPartPtr, encodedValue);

					if ((flags & sccoredb.Mdb_DataValueFlag_Exceptional) == 0) {
						Decimal d(decimalPart[0], decimalPart[1], decimalPart[2],
						(decimalPart[3] & 0x80000000) != 0, (Byte)(decimalPart[3] >> 16));
					}
				}
			}

			//throw ErrorCode.ToException(sccoredb.Mdb_GetLastError());
		}

		/// <summary>
		/// 
		/// </summary>
		public static bool ClrDecimalToEncodedX6DecimalTest() {
			Decimal clrDecimal = 0;
			Int64 encodedX6Decimal;

			unsafe {
				Int32[] decimalPart = Decimal.GetBits(clrDecimal);
				encodedX6Decimal = 0;

				fixed (Int32* decimalPartPtr = decimalPart) {
					// clr_decimal_to_encoded_x6_decimal() will do the conversion, and if the value fits
					// without data loss, the value will be written to encodedX6Decimal.
					return clr_decimal_to_encoded_x6_decimal(decimalPartPtr, ref encodedX6Decimal) == 0;
				}
			}
		}
#endif
    }
}
