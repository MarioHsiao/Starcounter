
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    [SuppressUnmanagedCodeSecurity]
    internal static class Kernel32
    {

        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        internal unsafe static extern void MoveByteMemory(Byte* Destination, Byte* Source, Int32 LengthBytes);
    }
}
