
using System;
namespace Starcounter.Internal {
    public class Utf8Helper {

        /// <summary>
        /// Writes UInt64 as UTF8.
        /// </summary>
        public static unsafe UInt32 WriteUIntAsUtf8(Byte* buf, UInt64 value)
        {
            UInt32 numBytes = 0;

            // Checking for zero value.
            if (value == 0) {
                buf[0] = (Byte)'0';
                return 1;
            }

            // Writing integers in reversed order.
            while (value != 0) {
                buf[numBytes++] = (Byte)(value % 10 + (Byte)'0');
                value = value / 10;
            }

            // Reversing the string.
            for (UInt32 k = 0; k < (numBytes / 2); k++)
            {
                byte t = buf[k];
                buf[k] = buf[numBytes - k - 1];
                buf[numBytes - k - 1] = t;
            }

            return numBytes;
        }

        /// <summary>
        /// Writes UInt64 as UTF8.
        /// </summary>
        public static unsafe UInt32 WriteUIntAsUtf8Man(Byte[] buf, UInt32 offset, UInt64 value)
        {
            UInt32 numBytes = 0;

            // Checking for zero value.
            if (value == 0) {
                buf[offset] = (Byte)'0';
                return 1;
            }

            // Writing integers in reversed order.
            while (value != 0) {
                buf[offset + numBytes++] = (Byte)(value % 10 + (Byte)'0');
                value = value / 10;
            }

            // Reversing the string.
            for (UInt32 k = 0; k < (numBytes / 2); k++)
            {
                Byte t = buf[offset + k];
                buf[offset + k] = buf[offset + numBytes - k - 1];
                buf[offset + numBytes - k - 1] = t;
            }

            return numBytes;
        }

        /// <summary>
        /// Parses integer from given buffer.
        /// </summary>
        public static Int64 IntFastParseFromAscii(Byte[] buf, Int32 offset, Int32 numChars)
        {
            Int64 mult = 1, result = 0;
            Boolean neg = false;

            Int32 start = offset;
            if (buf[offset] == (Byte)'-')
            {
                neg = true;
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

            if (neg)
                result = -result;

            return result;
        }

        /// <summary>
        /// Tries to parse integer from ASCII pointed string.
        /// </summary>
        public static Boolean IntFastParseFromAscii(IntPtr ptr, Int32 numChars, out Int64 result)
        {
            Int64 mult = 1;
            result = 0;
            Boolean neg = false;

            unsafe
            {
                Byte* start = (Byte*)ptr;
                if (*start == (Byte)'-')
                    neg = true;
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

                if (neg)
                    result = -result;
            }

            return true;
        }
    }
}
