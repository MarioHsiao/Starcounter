using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starcounter.Internal {
    /// <summary>
    /// Contains methods to write and read Boolean suitable for any encoding, since fits 16 bits
    /// </summary>
    public static class AnyBaseBool {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static void WriteBoolean(byte* buffer, Boolean value) {
#if true
            Base16Int.WriteBase16x1(*((byte*)&value), buffer);
#else
            if (value)
                Base16Int.WriteBase16x1(1, buffer);
            else
                Base16Int.WriteBase16x1(0, buffer);
#endif
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static Boolean ReadBoolean(byte* buffer) {
#if false
            Boolean val = false;
            if (Base16Int.ReadBase16x1((Base16x1*)buffer) == 1)
                val = true;
            return val;
#else
            var val = Base16Int.ReadBase16x1((Base16x1*)buffer);
            return *((bool*)&val);
#endif
        }

        public unsafe static int WriteBooleanNullable(byte* buffer, Boolean? value) {
            if (value == null)
                return 0;
            WriteBoolean(buffer, (bool)value);
            return 1;
        }

        public unsafe static int MeasureNeededSizeNullable(Boolean? value) {
            if (value == null)
                return 0;
            else
                return 1;
        }

        public unsafe static Boolean? ReadBooleanNullable(int size, byte* buffer) {
            if (size == 0)
                return null;
            Debug.Assert(size == 1);
            return ReadBoolean(buffer);
        }
    }
}
