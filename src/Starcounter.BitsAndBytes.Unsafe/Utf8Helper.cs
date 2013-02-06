
using System;
namespace Starcounter.Internal {
    public class Utf8Helper {

        public static unsafe uint WriteUIntAsUtf8(byte* buf, UInt64 value) {
            uint numBytes = 0;

            // Checking for zero value.
            if (value == 0) {
                buf[0] = (byte)'0';
                return 1;
            }

            // Writing integers in reversed order.
            while (value != 0) {
                buf[numBytes++] = (byte)(value % 10 + 0x30);
                value = value / 10;
            }

            // Reversing the string.
            for (uint k = 0; k < (numBytes / 2); k++) {
                byte t = buf[k];
                buf[k] = buf[numBytes - k - 1];
                buf[numBytes - k - 1] = t;
            }

            return numBytes;
        }

        public static unsafe uint WriteUIntAsUtf8Man(byte[] buf, uint offset, ulong value) {
            uint numBytes = 0;

            // Checking for zero value.
            if (value == 0) {
                buf[offset] = (byte)'0';
                return 1;
            }

            // Writing integers in reversed order.
            while (value != 0) {
                buf[offset + numBytes++] = (byte)(value % 10 + 0x30);
                value = value / 10;
            }

            // Reversing the string.
            for (uint k = 0; k < (numBytes / 2); k++) {
                byte t = buf[offset + k];
                buf[offset + k] = buf[offset + numBytes - k - 1];
                buf[offset + numBytes - k - 1] = t;
            }

            return numBytes;
        }

        static ulong[] mults = { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000, 10000000000, 100000000000, 1000000000000 };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="buf"></param>
        /// <param name="offset"></param>
        /// <param name="numChars"></param>
        /// <returns></returns>
        /// <remarks>
        /// TODO! Make version that handles signed integers
        /// </remarks>
        public static ulong IntFastParseFromAscii(byte[] buf, uint offset, uint numChars) {
            ulong result = 0, pos = offset + numChars - 1;

            for (int i = 0; i < numChars; i++) {
                result += ((ulong)buf[pos] - 0x30) * (mults[i]);
                pos--;
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ptr"></param>
        /// <param name="numChars"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IntFastParseFromAscii(IntPtr ptr, int numChars, out ulong value) {
            bool success = true;
            int i = numChars - 1;
            ulong result = 0;
            ulong mult = 1;


            unsafe {
                byte* bptr = (byte*)ptr;

                while (true) {
                    if (!(bptr[i] >= 48 && bptr[i] <= 57)) {
                        success = false;
                        break;
                    }

                    result += ((ulong)bptr[i] - '0') * mult;
                    --i;
                    if (i < 0)
                        break;

                    mult *= 10;
                }
            }

            value = result;
            return success;
        }
    }
}
