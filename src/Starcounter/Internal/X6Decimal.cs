using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Implementation of Starcounter decimal format and conversion to and from .Net decimal. 
    /// Specification of decimal format: http://www.starcounter.com/internal/wiki/Slots#Decimal
    /// </summary>
    public static class X6Decimal {
        public static readonly long MaxValue = 4398046511103999999;
        public static readonly long MinValue = -4398046511103999999;
        public const decimal MaxDecimalValue = 4398046511103.999999m;
        public const decimal MinDecimalValue = -4398046511103.999999m;
     
        private static readonly decimal[] scale_conversions = {
            1m,         // scale is already 6. Will never be used.
            1.0m,       
            1.00m,      
            1.000m,     
            1.0000m,    
            1.00000m,   
            1.000000m
        };

        /// <summary>
        /// Converts this X6Decimal to a .Net decimal
        /// </summary>
        /// <returns></returns>
        public static decimal ToDecimal(long encodedValue) {
            decimal dec;
            int signAndScale;
            
            unsafe {
                // scale is always 6, which converted to scale bit in decimal is 
                // 393216 (bits 16-23) without the sign bit set.
                signAndScale = (int)((encodedValue >> 32) & 0x80000000); // sign, bit 32
                signAndScale |= 0x60000; // scale
                ((int*)&dec)[0] = signAndScale;
                ((long*)&dec)[1] = (encodedValue & 0x7FFFFFFFFFFFFFFF);
            }

            return dec;
        }

        /// <summary>
        /// Converts a .Net decimal to a X6Decimal. Exception will be thrown on dataloss.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long FromDecimal(decimal value) {
            int scale;
            long encValue;
            long valueWithoutSign;

            unsafe {
                // Reading scale. If scale differs from 6 we need to adjust it.
                scale = ((int*)&value)[0];
                scale = (scale >> 16) & 0xFF;

                if (scale < 6) {
                    value *= scale_conversions[6 - scale];
                } else if (scale > 6) {
                    var roundedDec = decimal.Round(value, 6);
                    if (roundedDec != value)
                        throw ErrorCode.ToException(Error.SCERRCLRDECTOX6DECRANGEERROR);
                    value = roundedDec;
                }

                // We dont care if it is a positive or negative number since only the sign bit will differ.
                valueWithoutSign = ((long*)&value)[1];
                if (valueWithoutSign > X6Decimal.MaxValue)
                    throw ErrorCode.ToException(Error.SCERRCLRDECTOX6DECRANGEERROR);

                encValue = ((((int*)&value)[0] & 0x80000000) << 32);
                encValue |= valueWithoutSign;
            }

            return encValue;
        }
    }
}
