
namespace Starcounter.Internal {

    /// <summary>
    /// Implementation of Starcounter decimal format and conversion to and from .Net decimal. 
    /// Specification of decimal format: http://www.starcounter.com/internal/wiki/Slots#Decimal
    /// </summary>
    public static class X6Decimal {
        public const long MaxValue = 4398046511103999999;
        public const long MinValue = -4398046511103999999;
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
        /// Converts the starcounter decimal in encoded format to a .Net decimal.
        /// </summary>
        /// <param name="encodedValue"></param>
        /// <returns></returns>
        public static decimal FromEncoded(long encodedValue) {
            return FromRaw(Decode(encodedValue));
        }

        /// <summary>
        /// Converts the starcounter decimal in raw format to a .Net decimal.
        /// </summary>
        /// <returns></returns>
        public static decimal FromRaw(long rawValue) {
            decimal dec;
            int signAndScale;

            unsafe {
                signAndScale = 0;
                if (rawValue < 0) {
                    rawValue = -rawValue;
                    signAndScale = 1 << 31; 
                }

                // scale is always 6, which converted to scale bit in decimal is 
                // 393216 (bits 16-23) without the sign bit set.
                signAndScale |= 0x60000; // scale

                ((int*)&dec)[0] = signAndScale;
                ((long*)&dec)[1] = (rawValue & 0x7FFFFFFFFFFFFFFF);
            }
            return dec;
        }

        /// <summary>
        /// Converts a .Net decimal to Starcounter decimal in encoded format.
        /// Exception will be thrown on dataloss.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ToEncoded(decimal value) {
            return Encode(ToRaw(value));
        }

        /// <summary>
        /// Converts a .Net decimal to a Starcounter decimal in raw format. 
        /// Exception will be thrown on dataloss.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static long ToRaw(decimal value) {
            int scale;
            long rawValue;
            ulong unsignedRaw;
            
            unsafe {
                int* pvalue = (int*)&value;
                
                // Reading scale. If scale differs from 6 we need to adjust it.
                scale = pvalue[0];
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
                unsignedRaw = ((ulong*)&value)[1];
                if ((pvalue[1] != 0) || (unsignedRaw > X6Decimal.MaxValue)) // high != 0 or value > x6 decimal max.
                    throw ErrorCode.ToException(Error.SCERRCLRDECTOX6DECRANGEERROR);

                rawValue = (long)unsignedRaw;
                if (((pvalue[0] & 0x80000000) != 0)) { // sign
                    rawValue = -rawValue;
                }
            }
            return rawValue;
        }

        /// <summary>
        /// Decodes the encoded starcounter decimal.
        /// </summary>
        /// <param name="encodedValue"></param>
        /// <returns>Starcounter decimal in raw format.</returns>
        public static unsafe long Decode(long encodedValue) {
            bool sign;
            ulong buffer;
            ulong temp;
            ulong output;
            long ret;

            sign = encodedValue < 0;
            if (sign)
                encodedValue = -encodedValue;

            buffer = (ulong)encodedValue;

            temp = (buffer & 0x3FFF);
            output = temp;
            buffer >>= 14;

            temp = (buffer & 0x7F);
            if (temp != 0) {
                temp *= 10000;
                output += temp;
            }
            buffer >>= 7;

            temp = buffer;
            if (temp != 0) {
                temp *= 1000000;
                output += temp;
            }

            ret = (long)output;
            if (sign)
                ret = -ret;

            return (long)ret;
        }

        /// <summary>
        /// Encodes the raw starcounter decimal.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>Starcounter decimal in encoded format.</returns>
        public static unsafe long Encode(long value) {
            bool sign;
            ulong restq;
            uint restd;
            uint divd;
            uint divw;
            ulong tempq;
            ulong tempd;
            ulong partq;
            ulong partd;
            byte shift;
            ulong buffer;
            long ret;

            if (value >= 0) {
                sign = false;
                restq = (ulong)value;
            } else {
                sign = true;
                restq = (ulong)(-value);
            }

            // The integer part.

            divd = 1000000;
            shift = (7 + 14);
            tempq = restq;
            partq = (tempq / divd);
            restd = (uint)(tempq % divd);
//            _SC_ASSERT_DEBUG(partq <= 0x000003FFFFFFFFFF);
            partq <<= shift;
            buffer = partq;

            // Decimal digits 1 and 2.

            divw = 10000;
            shift = 14;
            tempd = restd;
            partd = (tempd / divw);
            restd = (uint)(tempd % divw);
            partd <<= shift;
            buffer |= partd;

            // Decimal digits 3 to 6.

            buffer |= restd;

            ret = (long)buffer;
            if (sign)
                ret = -ret;

            return ret;
        }
    }
}
