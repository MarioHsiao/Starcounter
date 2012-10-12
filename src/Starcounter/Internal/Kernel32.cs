
using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Starcounter.Internal
{
    [SuppressUnmanagedCodeSecurity]
    public static class Kernel32
    {

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern void ExitProcess(uint exitCode);
        
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi, EntryPoint = "RtlMoveMemory")]
        internal unsafe static extern void MoveByteMemory(Byte* Destination, Byte* Source, Int32 LengthBytes);

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public static extern int SetProcessPriorityBoost(IntPtr hProcess, int DisablePriorityBoost);

        public const uint MEM_COMMIT = 0x00001000;

        public const uint PAGE_READWRITE = 0x04;

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        public unsafe static extern void *VirtualAlloc(void *lpAddress, IntPtr dwSize, uint flAllocationType, uint flProtect);
    }
}
