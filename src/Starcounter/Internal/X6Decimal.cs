using System;

namespace Starcounter.Internal {

    /// <summary>
    /// Implementation of Starcounter decimal format and conversion to and from .Net decimal. 
    /// Specification of decimal format: http://www.starcounter.com/internal/wiki/Slots#Decimal
    /// </summary>
    public struct X6Decimal {
        // layout of .Net Decimal (all int)
        // --------------------------
        // flags (sign and scale)
        // high
        // mid
        // low
        private const long maxEncodedInt = 4398046511103999999;

        public static readonly X6Decimal MaxValue = 4398046511103999999;
        public static readonly X6Decimal MinValue = -4398046511103999999;
        public static readonly X6Decimal Zero = 0;
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
        /// The raw encoded value in Starcounter X6 decimal format.
        /// </summary>
        private long encValue;

        /// <summary>
        /// Creates a new instance of X6Decimal based on the encoded long value.
        /// </summary>
        /// <param name="encValue"></param>
        public X6Decimal(long encValue) {
            this.encValue = encValue;
        }

        /// <summary>
        /// Implicit conversion to long from a X6Decimal
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator long(X6Decimal value) {
            return value.encValue;
        }

        /// <summary>
        /// Implicit conversion to X6Decimal from long.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static implicit operator X6Decimal(long value) {
            X6Decimal ret;
            ret.encValue = value;
            return ret;
        }

        /// <summary>
        /// Gets the raw encoded value.
        /// </summary>
        public long EncodedValue { get { return encValue; } }

        /// <summary>
        /// Converts this X6Decimal to a .Net decimal
        /// </summary>
        /// <returns></returns>
        public decimal ToDecimal() {
            decimal dec;
            int signAndScale;
            
            unsafe {
                // scale is always 6, which converted to scale bit in decimal is 
                // 393216 (bits 16-23) without the sign bit set.
                signAndScale = (int)((encValue >> 32) & 0x80000000); // sign, bit 32
                signAndScale |= 0x60000; // scale
                ((int*)&dec)[0] = signAndScale; 
                ((long*)&dec)[1] = (encValue & 0x7FFFFFFFFFFFFFFF);
            }

            return dec;
        }

        /// <summary>
        /// Converts a .Net decimal to a X6Decimal. Exception will be thrown on dataloss.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static X6Decimal FromDecimal(decimal value) {
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
                if (valueWithoutSign > X6Decimal.maxEncodedInt)
                    throw ErrorCode.ToException(Error.SCERRCLRDECTOX6DECRANGEERROR);

                encValue = ((((int*)&value)[0] & 0x80000000) << 32);
                encValue |= valueWithoutSign;
            }

            return encValue;
        }
    }
}
