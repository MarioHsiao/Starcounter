
using System;
namespace Starcounter.Internal {
    public class Utf8Helper {

        /// <summary>
        /// Writes Int64 as UTF8.
        /// </summary>
        public static unsafe UInt32 WriteIntAsUtf8(Byte* buf, Int64 value)
        {
            // Checking for zero value.
            if (0 == value)
            {
                buf[0] = (Byte)'0';
                return 1;
            }

            // Checking if number is negative.
            UInt32 extra_symbols = 0;
            Byte first_digit;
            if (value < 0)
            {
                buf[0] = (Byte)'-';
                buf++;
                extra_symbols = 1;
                first_digit = (Byte)(-(value % 10));
                value = value / 10;
                value = -value;
            }
            else
            {
                first_digit = (Byte)(value % 10);
                value = value / 10;
            }

            // Writing first digit.
            UInt32 num_digits = 0;
            buf[num_digits++] = (Byte)(first_digit + (Byte)'0');

            // Writing integers in reversed order.
            while (value != 0)
            {
                buf[num_digits++] = (Byte)(value % 10 + (Byte)'0');
                value = value / 10;
            }

            // Reversing the string.
            for (UInt32 k = 0; k < (num_digits / 2); k++)
            {
                Byte t = buf[k];
                buf[k] = buf[num_digits - k - 1];
                buf[num_digits - k - 1] = t;
            }

            return extra_symbols + num_digits;
        }

        /// <summary>
        /// Writes Int64 as UTF8.
        /// </summary>
        public static unsafe UInt32 WriteIntAsUtf8Man(Byte[] buf, UInt32 offset, Int64 value)
        {
            // Checking for zero value.
            if (0 == value)
            {
                buf[offset] = (Byte)'0';
                return 1;
            }

            // Checking if number is negative.
            UInt32 extra_symbols = 0;
            Byte first_digit;
            if (value < 0)
            {
                buf[offset] = (Byte)'-';
                offset++;
                extra_symbols = 1;
                first_digit = (Byte) (-(value % 10));
                value = value / 10;
                value = -value;
            }
            else
            {
                first_digit = (Byte)(value % 10);
                value = value / 10;
            }

            // Writing first digit.
            UInt32 num_digits = 0;
            buf[offset + num_digits++] = (Byte)(first_digit + (Byte)'0');

            // Writing integers in reversed order.
            while (value != 0)
            {
                buf[offset + num_digits++] = (Byte)(value % 10 + (Byte)'0');
                value = value / 10;
            }

            // Reversing the string.
            for (UInt32 k = 0; k < (num_digits / 2); k++)
            {
                Byte t = buf[offset + k];
                buf[offset + k] = buf[offset + num_digits - k - 1];
                buf[offset + num_digits - k - 1] = t;
            }

            return extra_symbols + num_digits;
        }

        /// <summary>
        /// Parses integer from given buffer.
        /// </summary>
        public static Int64 IntFastParseFromAscii(Byte[] buf, Int32 offset, Int32 numChars)
        {
            Int64 mult = 1, result = 0;

            Int32 start = offset;
            if (buf[offset] == (Byte)'-')
            {
                mult = -1;
                start++;
            }

            Int32 cur = offset + numChars - 1;
            do
            {
                result += mult * (buf[cur] - (Byte)'0');
                mult *= 10;
                cur--;
            }
            while (cur >= start);

            return result;
        }

        /// <summary>
        /// Tries to parse integer from ASCII pointed string.
        /// </summary>
        public static Boolean IntFastParseFromAscii(IntPtr ptr, Int32 numChars, out Int64 result)
        {
            Int64 mult = 1;
            result = 0;

            unsafe
            {
                Byte* start = (Byte*)ptr;
                if (*start == (Byte)'-')
                    mult = -1;
                else
                    start--;

                Byte* cur = (Byte*)ptr + numChars - 1;
                do
                {
                    Byte curb = *cur;

                    // Checking if every character is a digit.
                    if ((curb > (Byte)'9') || (curb < (Byte)'0'))
                        return false;

                    result += mult * (curb - (Byte)'0');
                    mult *= 10;
                    cur--;
                }
                while (cur > start);
            }

            return true;
        }
    }
}
