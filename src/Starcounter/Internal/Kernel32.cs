
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    [SuppressUnmanagedCodeSecurity]
    public static class Kernel32
    {

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        internal static extern void ExitProcess(uint exitCode);
        
        [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
        internal unsafe static extern void MoveByteMemory(Byte* Destination, Byte* Source, Int32 LengthBytes);

        [DllImport("Kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("Kernel32.dll")]
        public static extern int SetProcessPriorityBoost(IntPtr hProcess, int DisablePriorityBoost);
    }
}
