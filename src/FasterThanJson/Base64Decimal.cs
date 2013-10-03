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
#if false
            int[] intVal = Decimal.GetBits(value);
            
            Debug.Assert(firstChar < 64);
            uint highInt;
            ulong lowLong;
            fixed (int* valPtr = intVal) {
                highInt = *(uint*)(valPtr);
                lowLong = *(ulong*)(valPtr + 1);
            }
#else
            Debug.Assert(BitConverter.IsLittleEndian);
            byte* byteValue = (byte*)&value;
            byte scale = *(byteValue + 2);
            byte sign = (byte)(*(byteValue + 3) >> 7);
            Debug.Assert((UInt16)(*byteValue) == 0);
            Debug.Assert(scale <= 28);
            Debug.Assert(sign == 0 || sign == 1);
            byte firstChar = (byte)((scale << 1) + sign);
            Debug.Assert(firstChar < 64);
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

    public static class Base64DecimalScLoss {
    }
}
