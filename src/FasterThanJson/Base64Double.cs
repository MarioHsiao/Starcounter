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
            return Base64Int.MeasureNeededSize(*(ulong*)&value << 12);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public static unsafe Double Read(int size, byte* buffer) {
            ulong value = Base64Int.Read(size, buffer);
            value = (value >> 12) | (value << 52);
            return *(Double*)&value;
        }
}

    public static class Base64Single {
    }
}
