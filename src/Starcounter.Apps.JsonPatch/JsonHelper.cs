using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Starcounter.Internal;

namespace Starcounter.Internal.JsonPatch {
    /// <summary>
    /// 
    /// </summary>
    public static class JsonHelper {
        private static unsafe int SizeToDelimiterOrEnd(byte* pfrag, int fragmentSize) {
            byte current;
            int index = 0;

            while (index < fragmentSize) {
                current = pfrag[index];

                if (current == ',' || current == ' '
                    || current == '}' || current == '\n'
                    || current == '\r')
                    break;
                index++;
            }
            return index;
        }

        private static unsafe int SizeToDelimiterOrEndString(byte* pfrag, int fragmentSize, out bool needsUrlDecoding) {
            byte current;
            int index = 0;

            needsUrlDecoding = false;
            while (index < fragmentSize) {
                current = pfrag[index];
                //if (current == '%') {
                //    needsUrlDecoding = true;
                //    index += 2;
                //    continue;
                //}

                if (pfrag[index] == '"')
                    break;
                index++;
            }
            return index;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string DecodeString(byte[] buffer) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks the size of the value and try to parse it as an int.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the value.</param>
        /// <param name="size">The size to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if value was succesfully parsed, <c>false</c> otherwise</returns>
        public static bool ParseInt(IntPtr ptr, int size, out int value, out int valueSize) {
            ulong result;

            unsafe {
                valueSize = SizeToDelimiterOrEnd((byte*)ptr, size);
            }

            if (Utf8Helper.IntFastParseFromAscii(ptr, valueSize, out result)) {
                value = (int)result;
                return true;
            }
            value = -1;
            return false;
        }

        /// <summary>
        /// Checks the size of the value and try to parse it as a decimal.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the value.</param>
        /// <param name="size">The size to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if value was succesfully parsed, <c>false</c> otherwise</returns>
        public static bool ParseDecimal(IntPtr ptr, int size, out decimal value, out int valueSize) {
            bool success;
            string valAsStr;

            // TODO:
            // Need to optimize this parsing. This method is superslow.

            value = 0.0m;
            success = false;
            if (ParseString(ptr, size, out valAsStr, out valueSize)) {
                success = decimal.TryParse(valAsStr, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
            return success;
        }

        /// <summary>
        /// Checks the size of the value and try to parse it as a double.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the value.</param>
        /// <param name="size">The size to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if value was succesfully parsed, <c>false</c> otherwise</returns>
        public static bool ParseDouble(IntPtr ptr, int size, out double value, out int valueSize) {
            bool success;
            string valAsStr;

            // TODO:
            // Need to optimize this parsing. This method is superslow.

            value = 0.0d;
            success = false;
            if (ParseString(ptr, size, out valAsStr, out valueSize)) {
                success = double.TryParse(valAsStr, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
            return success;
        }

        /// <summary>
        /// Checks the size of the value and try to parse it as a boolean.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the value.</param>
        /// <param name="size">The size to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if value was succesfully parsed, <c>false</c> otherwise</returns>
        public static bool ParseBoolean(IntPtr ptr, int size, out bool value, out int valueSize) {
            bool success = false;

            value = false;
            valueSize = -1;
            if (size != 0) {
                unsafe {
                    byte* p = (byte*)ptr;

                    switch (*p) {
                        case (byte)'t':
                            if (size < 4)
                                break;

                            if (p[1] == 'r' && p[2] == 'u' && p[3] == 'e') {
                                value = true;
                                valueSize = 4;
                                success = true;
                            }
                            break;
                        case (byte)'f':
                            if (size < 5)
                                break;

                            if (p[1] == 'a' && p[2] == 'l' && p[3] == 's' && p[4] == 'e') {
                                valueSize = 5;
                                success = true;
                            }
                            break;
                    }
                }
            }
            return success;
        }

        /// <summary>
        /// Checks the size of the value and try to parse it as a DateTime.
        /// </summary>
        /// <param name="ptr">Pointer to the start of the value.</param>
        /// <param name="size">The size to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if value was succesfully parsed, <c>false</c> otherwise</returns>
        public static bool ParseDateTime(IntPtr ptr, int size, out DateTime value, out int valueSize) {
            bool success;
            string valAsStr;

            // TODO:
            // Need to optimize this parsing. This method is superslow.

            value = DateTime.MinValue;
            success = false;
            if (ParseString(ptr, size, out valAsStr, out valueSize)) {
                success = DateTime.TryParse(valAsStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
            }
            return success;
        }


        /// <summary>
        /// Parses the string and decodes it if needed.
        /// </summary>
        /// <param name="ptr">A pointer to the current position in the buffer.</param>
        /// <param name="size">The size left to the end of the buffer.</param>
        /// <param name="value">The parsed value.</param>
        /// <param name="valueSize">The size of the unparsed value in bytes</param>
        /// <returns><c>true</c> if parsing was succesful, <c>false</c> otherwise</returns>
        public static bool ParseString(IntPtr ptr, int size, out string value, out int valueSize) {
            bool needsDecoding;
            byte[] buffer;
            int extraSize = 0;

            unsafe {
                byte* pfrag = (byte*)ptr;

                if (*pfrag == '"') { // Value is enclosed in start and stop quote, "value".
                    pfrag++;
                    size--;
                    extraSize = 2;
                    valueSize = SizeToDelimiterOrEndString(pfrag, size, out needsDecoding);
                } else {
                    needsDecoding = false;
                    valueSize = SizeToDelimiterOrEnd(pfrag, size);
                }

                buffer = new byte[valueSize];
                fixed (byte* pbuf = buffer) {
                    Intrinsics.MemCpy((void*)pbuf, (void*)pfrag, (uint)valueSize);
                }
            }

            if (needsDecoding) {
                value = DecodeString(buffer);
            } else {
                value = Encoding.UTF8.GetString(buffer, 0, valueSize);
            }

            valueSize += extraSize;
            return true;
        }
    }
}
