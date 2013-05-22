using System;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace Starcounter.Tests {
	/// <summary>
	/// 
	/// </summary>
	public static class TestDecimal {
		/// <summary>
		/// Function used by TestDecimal().
		/// </summary>
		[DllImport("decimal_conversion.dll", CallingConvention = CallingConvention.StdCall)]
		public unsafe extern static UInt32 clr_decimal_to_encoded_x6_decimal
		(Int32* decimal_part_ptr, ref Int64 encoded_x6_decimal_ptr);
		
		/// <summary>
		/// 
		/// </summary>
		[Test]
		public static void DecimalTest() {
			decimal a = 1;
			decimal b = 1;
			Assert.AreEqual(a, b);
			//Assert.True(false);
		}
    }
}
