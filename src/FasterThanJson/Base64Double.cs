using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starcounter.Internal {
    /// <summary>
    /// The input format of Double is
    /// 0-51 bits - mantissa
    /// 52-62 bits - exponent
    /// 63 bit - sign
    /// The serialization format is at least 3 bytes:
    /// 2 bytes contains 12 bits:
    ///   0 bit - sign
    ///   1-11 bits - exponent
    /// 1 or more bytes contains mantissa (up to 11 bytes until 9 byte representation of UInt64  is implemented)
    /// 
    /// Nullable value will be stored as a single byte.
    /// </summary>
    public static class Base64Double {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe int Write(byte* buffer, Double value) {
            ulong valueUInt = *(ulong*)&value;
            ulong encValue = (valueUInt << 12) | (valueUInt >> 52);
            int len = Base64Int.Write(buffer, encValue);
            Debug.Assert(valueUInt == ((encValue >> 12) | (encValue << 52)));
            return len;
        }

        public static unsafe int MeasureNeededSize(Double value) {
            ulong valueUInt = *(ulong*)&value;
            return Base64Int.MeasureNeededSize((valueUInt << 12) | (valueUInt >> 52));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe Double Read(int size, byte* buffer) {
            ulong value = Base64Int.Read(size, buffer);
            value = (value >> 12) | (value << 52);
            return *(Double*)&value;
        }

        public static unsafe int WriteNullable(byte* buffer, Double? value) {
            if (value == null) {
                Base16Int.WriteBase16x1(1, buffer);
                return 1;
            } else {
                Base16Int.WriteBase16x1(0, buffer);
                return 1 + Write(buffer + 1, (Double)value);
            }
        }

        public static unsafe int MeasureNeededSizeNullable(Double? value) {
            if (value == null)
                return 1;
            else
                return 1 + MeasureNeededSize((Double)value);
        }

        public static unsafe Double? ReadNullable(int size, byte* buffer) {
            if (size > 1) {
                Debug.Assert(Base16Int.ReadBase16x1((Base16x1*)buffer) == 0);
                return Read(size - 1, buffer + 1);
            } else {
                Debug.Assert(Base16Int.ReadBase16x1((Base16x1*)buffer) == 1);
                return null;
            }
        }
    }

    public static class Base64Single {
        public static unsafe int Write(byte* buffer, Single value) {
            uint valueUInt = *(uint*)&value;
            uint encValue = (valueUInt << 9) | (valueUInt >> 23);
            int len = Base64Int.Write(buffer, encValue);
            Debug.Assert(valueUInt == ((encValue >> 9) | (encValue << 23)));
            return len;
        }

        public static unsafe int MeasureNeededSize(Single value) {
            uint valueUInt = *(uint*)&value;
            return Base64Int.MeasureNeededSize((valueUInt << 9) | (valueUInt >> 23));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe Single Read(int size, byte* buffer) {
            ulong value = Base64Int.Read(size, buffer);
            value = (value >> 9) | (value << 23);
            return *(Single*)&value;
        }
    }
}
