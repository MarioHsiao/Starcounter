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
#if false // Too slow
            int[] intVal = Decimal.GetBits(value);
            uint sign = (uint)intVal[3] >> 31;
            uint firstChar = (((uint)(intVal[3]) & 0x00FF0000) >> 15) + sign;
            Debug.Assert((intVal[3] & 0x7F00FFFF) == 0);
            Debug.Assert(sign == 0 || sign == 1);
            Debug.Assert((firstChar >> 1) <= 28);
            Debug.Assert(firstChar < 64);
            uint highInt = (uint)intVal[2];
            ulong lowLong = (ulong)intVal[0];
            if (intVal[1] != 00)
                lowLong += (ulong)(intVal[1]) << 32;
#if false
            ulong lowNumber = (ulong)intVal[0];
            ulong highNumber = 0;
            if (intVal[1] != 0) {
                lowNumber += (((ulong)intVal[1] & 0xFFFF) << 32);
                highNumber = ((ulong)intVal[1] >> 16);
            }
            if (intVal[2] != 0)
                highNumber += ((ulong)intVal[2] << 16);
#endif
#else
            Debug.Assert(BitConverter.IsLittleEndian);
            byte* byteValue = (byte*)&value;
            Debug.Assert(*(UInt16*)(byteValue) == 0);
#if false // Same performance
            byte scale = *(byteValue + 2);
            byte sign = (byte)(*(byteValue + 3) >> 7);
            Debug.Assert(scale <= 28);
            Debug.Assert(sign == 0 || sign == 1);
            byte firstChar = (byte)((scale << 1) + sign);
#else
            ushort firstChar = *(ushort*)(byteValue + 2);
            firstChar = (ushort)((firstChar << 1) | (firstChar >> 15));
            Debug.Assert((firstChar >> 1) <= 28);
            Debug.Assert((firstChar & 0x1) == 0 || (firstChar & 0x1) == 1);
            Debug.Assert(firstChar < 64);
#endif
            uint highInt = *(uint*)(byteValue + 4);
            ulong lowLong = *(ulong*)(byteValue + 8);

#endif
            // Writing
            Base64Int.WriteBase64x1(firstChar, buffer);
            buffer++;
            int len = 1;
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
            return len;
        }

        public unsafe static int MeasureNeededSize(decimal value) {
            byte* byteValue = (byte*)&value;
            uint highInt = *(uint*)(byteValue + 4);
            ulong lowLong = *(ulong*)(byteValue + 8);
            int size = 1;
            if (highInt == 0)
                size += Base64Int.MeasureNeededSize(lowLong);
            else
                size += 11 + Base64Int.MeasureNeededSize(highInt);
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static decimal Read(int size, byte* buffer) {
            Debug.Assert(size > 1);
            byte firstChar = (byte)Base64Int.ReadBase64x1(buffer);
            size--;
            buffer++;
            int sign = firstChar & 0x01;
            Debug.Assert(sign == 0 || sign == 1);
            int scale = firstChar >> 1;
            Debug.Assert(scale <= 28);
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
            Decimal newValue = 0m;
            byte* byteValue = (byte*)&newValue;
            *(byteValue + 2) = (byte)scale;
            *(byteValue + 3) = (byte)(sign << 7);
            *((uint*)(byteValue + 4)) = (uint)highInt;
            *((ulong*)(byteValue + 8)) = lowLong;
            return newValue;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static decimal Read(int size, byte* buffer) {
            Debug.Assert(BitConverter.IsLittleEndian);
            ulong readValue = Base64Int.Read(size, buffer);
            uint sign = (uint)((readValue & 0x1) << 31);
            Decimal newValue = 1.000000m;
            *(uint*)&newValue |= sign;
            *((ulong*)&newValue + 1) = readValue >> 1;
            return newValue;
        }
    }
}
