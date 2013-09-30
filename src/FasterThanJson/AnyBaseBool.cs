using System;
using System.Runtime.CompilerServices;

namespace Starcounter.Internal {
    /// <summary>
    /// Contains methods to write and read Boolean suitable for any encoding, since fits 16 bits
    /// </summary>
    public static class AnyBaseBool {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static void WriteBoolean(byte* buffer, Boolean value) {
            if (value)
                Base16Int.WriteBase16x1(1, buffer);
            else
                Base16Int.WriteBase16x1(0, buffer);

        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static void WriteBooleanNullable(byte* buffer, Boolean? value) {
            if (value == null)
                Base16Int.WriteBase16x1(2, buffer);
            else if ((bool)value)
                Base16Int.WriteBase16x1(1, buffer);
            else
                Base16Int.WriteBase16x1(0, buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static Boolean ReadBoolean(byte* buffer) {
            Boolean val = false;
            if (Base16Int.ReadBase16x1((Base16x1*)buffer) == 1)
                val = true;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Available starting with .NET framework version 4.5
        public unsafe static Boolean? ReadBooleanNullable(byte* buffer) {
            Boolean? val = false;
            var intVal = Base16Int.ReadBase16x1((Base16x1*)buffer);
            if (intVal == 1)
                val = true;
            else if (intVal == 2)
                val = null;
            return val;
        }
    }
}
