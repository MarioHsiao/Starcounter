using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starcounter.Internal {
    /// <summary>
    /// Decimal: 1 byte - 0-28 precision, 3 4-bytes (3 ints) - value, sign (http://msdn.microsoft.com/en-us/library/vstudio/system.decimal.getbits.aspx)
    /// First letter contains 
    /// </summary>
    public static class Base64DecimalLossless {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static int Write(byte* buffer, Decimal value) {
            Debug.Assert(BitConverter.IsLittleEndian);
            uint* ptrValue = (uint*)&value;
            // Scale and sign
            Debug.Assert(*(UInt16*)(ptrValue) == 0);
            ushort firstChar = (ushort)((*ptrValue >> 15) | (*ptrValue >> 31));
            Debug.Assert((firstChar >> 1) <= 28);
            Debug.Assert((firstChar & 0x1) == 0 || (firstChar & 0x1) == 1);
            Debug.Assert(firstChar < 64);
            Base64Int.WriteBase64x1(firstChar, buffer);
            buffer++;
            int len = 1;

            // The value
#if false
            uint highInt = *(ptrValue + 1);
            ulong lowLong = *(ulong*)(ptrValue + 2);

            if (highInt == 0) {
                len += Base64Int.Write(buffer, lowLong);
                Debug.Assert(len <= 1 + 11);
            } else {
                Base64Int.WriteBase64x11(lowLong, buffer);
                buffer += 11;
                len += 11;
                len += Base64Int.Write(buffer, highInt);
                Debug.Assert(len <= 1 + 11 + 6);
            }
#else
            ulong low48bits6bytes = *(ulong*)(ptrValue + 2) & 0xFFFFFFFFFFFF;
            ulong high48bits = (*(ptrValue + 3) >> 16) | ((ulong)*(ptrValue+1) << 16);
            Debug.Assert((low48bits6bytes >> 48) == 0);
            Debug.Assert((high48bits >> 48) == 0);
            Debug.Assert(*(ptrValue + 1) == (high48bits >> 16));
            Debug.Assert(*(ptrValue + 2) == (low48bits6bytes & 0xFFFFFFFF));
            Debug.Assert(*(ptrValue + 3) == ((low48bits6bytes >> 32) | (uint)(high48bits << 16)));
            if (high48bits == 0) {
                len += Base64Int.Write(buffer, low48bits6bytes);
                Debug.Assert(len <= 1 + 8); 
            } else {
                Base64Int.WriteBase64x8(low48bits6bytes, buffer);
                buffer += 8;
                len += 8;
                len += Base64Int.Write(buffer, high48bits);
                Debug.Assert(len <= 1 + 8 + 8);
            }
#endif
            return len;
        }

        public unsafe static int MeasureNeededSize(decimal value) {
            int size = 1;
#if false
            byte* byteValue = (byte*)&value;
            uint highInt = *(uint*)(byteValue + 4);
            ulong lowLong = *(ulong*)(byteValue + 8);
            if (highInt == 0)
                size += Base64Int.MeasureNeededSize(lowLong);
            else
                size += 11 + Base64Int.MeasureNeededSize(highInt);
#else
            uint* ptrValue = (uint*)&value;
            ulong low48bits6bytes = *(ulong*)(ptrValue + 2) & 0xFFFFFFFFFFFF;
            ulong high48bits = (*(ptrValue + 3) >> 16) | ((ulong)*(ptrValue + 1) << 16);
            if (high48bits == 0)
                size += Base64Int.MeasureNeededSize(low48bits6bytes);
            else
                size += 8 + Base64Int.MeasureNeededSize(high48bits);
#endif
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static decimal Read(int size, byte* buffer) {
            // Sign and scale
            Debug.Assert(size > 1);
            uint firstChar = (uint)Base64Int.ReadBase64x1(buffer);
            size--;
            buffer++;
            uint sign = firstChar & 0x01;
            Debug.Assert(sign == 0 || sign == 1);
            uint scale = firstChar >> 1;
            Debug.Assert(scale <= 28);
            Decimal newValue = 0m;
            uint* ptrValue = (uint*)&newValue;
            *ptrValue = (sign << 31) | (scale << 16);
            Debug.Assert((*ptrValue >> 31) == sign);
            Debug.Assert((*ptrValue & 0x7F00FFFF) == 0);

            // The value
#if false
            ulong highInt = 0;
            ulong lowLong;
            if (size > 11) {
                lowLong = Base64Int.ReadBase64x11(buffer);
                size -= 11;
                buffer += 11;
                highInt = Base64Int.Read(size, buffer);
                Debug.Assert(highInt <= uint.MaxValue);
            } else
                lowLong = Base64Int.Read(size, buffer);
            *(ptrValue + 1) = (uint)highInt;
            *((ulong*)(ptrValue + 2)) = lowLong;
#else
            ulong low6bytes = 0;
            ulong high6bytes = 0;
            if (size > 8) { 
                low6bytes = Base64Int.ReadBase64x8(buffer);
                size -= 8;
                buffer += 8;
                high6bytes = Base64Int.Read(size, buffer);
                *(ptrValue + 1) = (uint)(high6bytes >> 16);
                *(ptrValue + 3) = (uint)high6bytes << 16;
            } else {
                low6bytes = Base64Int.Read(size, buffer);
            }
            *(ulong*)(ptrValue + 2) |= low6bytes;
#endif
            return newValue;
        }

        public unsafe static int WriteNullable(byte* buffer, Decimal? value) {
            if (value == null)
                return 0;
            return Write(buffer, (Decimal)value);
        }

        public unsafe static decimal? ReadNullable(int size, byte* buffer) {
            if (size == 0)
                return null;
            return Read(size, buffer);
        }

        public unsafe static int MeasureNeededSizeNullable(decimal? value) {
            if (value == null)
                return 0;
            return MeasureNeededSize((decimal)value);
        }
    }

    public static class Base64X6Decimal {
        /// <summary>
        /// Encodes decimal value assuming that it was converted from X6Decimal and
        /// writes into the given tuple
        /// </summary>
        /// <param name="buffer">Place to write</param>
        /// <param name="value">Decimal with 6 digits scale as converted from X6Decimal</param>
        /// <returns>Number of bytes written</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static int Write(byte* buffer, Decimal value) {
            Debug.Assert(BitConverter.IsLittleEndian);
            uint* uintValue = (uint*)&value;
            ulong sign = *uintValue >> 31;
            ulong lowLong = *(ulong*)(uintValue + 2);
            Debug.Assert(*(UInt16*)&value == 0);
            Debug.Assert(*((byte*)&value + 2) == 6);
            Debug.Assert(sign == 0 || sign == 1);
            Debug.Assert(*(uintValue + 1) == 0);
            Debug.Assert((lowLong >> 63) == 0);
            return Base64Int.Write(buffer, (lowLong << 1) | sign);
        }

        public unsafe static int MeasureNeededSize(decimal value) {
            return Base64Int.MeasureNeededSize(*((ulong*)&value + 1) << 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static decimal Read(int size, byte* buffer) {
            Debug.Assert(BitConverter.IsLittleEndian);
            ulong readValue = Base64Int.Read(size, buffer);
            uint sign = (uint)(readValue & 0x1) << 31;
            Decimal newValue = 1.000000m;
            *(uint*)&newValue |= sign;
            *((ulong*)&newValue + 1) = readValue >> 1;
            return newValue;
        }
    }
}
